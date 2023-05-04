using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides.Quizzes;
using Ulearn.Core.Courses.Slides.Quizzes.Blocks;
using Ulearn.Core.Extensions;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Controllers;

[Route("/export")]
public class ExportController : BaseController
{
	private readonly ICourseRolesRepo courseRolesRepo;
	private readonly IGroupMembersRepo groupMembersRepo;
	private readonly IGroupsRepo groupsRepo;
	private readonly IUnitsRepo unitsRepo;
	private readonly IUserQuizzesRepo userQuizzesRepo;
	private readonly IVisitsRepo visitsRepo;

	public ExportController(ICourseStorage courseStorage, UlearnDb db, IUsersRepo usersRepo,
		IGroupMembersRepo groupMembersRepo, IVisitsRepo visitsRepo, IGroupsRepo groupsRepo, IUserQuizzesRepo userQuizzesRepo,
		ICourseRolesRepo courseRolesRepo, IUnitsRepo unitsRepo)
		: base(courseStorage, db, usersRepo)
	{
		this.groupMembersRepo = groupMembersRepo;
		this.visitsRepo = visitsRepo;
		this.groupsRepo = groupsRepo;
		this.userQuizzesRepo = userQuizzesRepo;
		this.courseRolesRepo = courseRolesRepo;
		this.unitsRepo = unitsRepo;
	}

	[HttpGet("users-info-and-results")]
	[Authorize]
	public async Task<ActionResult> ExportGroupMembersAsTsv([Required] int groupId, Guid? quizSlideId = null)
	{
		var group = await groupsRepo.FindGroupByIdAsync(groupId);
		if (group is null)
			return StatusCode((int)HttpStatusCode.NotFound, "Group not found");

		var isSystemAdministrator = await IsSystemAdministratorAsync();
		var isCourseAdmin = await courseRolesRepo.HasUserAccessToCourse(UserId, group.CourseId, CourseRoleType.CourseAdmin);

		if (!(isSystemAdministrator || isCourseAdmin))
			return StatusCode((int)HttpStatusCode.Forbidden, "You should be course or system admin");

		var users = await GetExtendedUserInfo(groupId);

		List<string> questions = null;
		var courseId = group.CourseId;
		var course = courseStorage.GetCourse(courseId);
		var visibleUnits = await unitsRepo.GetPublishedUnitIds(course);
		if (quizSlideId is not null)
		{
			var slide = course.FindSlideById(quizSlideId.Value, false, visibleUnits);
			if (slide is null)
				return StatusCode((int)HttpStatusCode.NotFound, $"Slide not found in course {courseId}");

			if (slide is not QuizSlide quizSlide)
				return StatusCode((int)HttpStatusCode.NotFound, $"Slide is not quiz slide in course {courseId}");

			(questions, var answers) = await GetQuizAnswers(users.Select(s => s.Id), courseId, quizSlide);
			users = users.Zip(answers, (u, a) =>
			{
				u.Answers = a;
				return u;
			}).ToList();
		}

		var slides = course.GetSlides(false, visibleUnits).Where(s => s.ShouldBeSolved).Select(s => s.Id).ToList();
		var scores = GetScoresByScoringGroups(users.Select(u => u.Id).ToList(), slides, course);
		var scoringGroupsWithScores = scores.Select(kvp => kvp.Key.ScoringGroup).ToHashSet();
		var scoringGroups = course.Settings.Scoring.Groups.Values.Where(sg => scoringGroupsWithScores.Contains(sg.Id)).ToList();

		var headers = new List<string> { "Id", "Login", "Email", "VkUrl", "Telegram", "FirstName", "LastName", "VisibleName", "Gender", "LastVisit", "IpAddress" };
		if (questions is not null)
			headers = headers.Concat(questions).ToList();
		if (scoringGroups.Count > 0)
			headers = headers.Concat(scoringGroups.Select(s => s.Abbreviation)).ToList();

		var rows = new List<List<string>> { headers };
		foreach (var i in users)
		{
			var row = new List<string> { i.Id, i.Login, i.Email, i.VkUrl, i.Telegram, i.FirstName, i.LastName, i.VisibleName, i.Gender.ToString(), i.LastVisit.ToSortableDate(), i.IpAddress };
			if (i.Answers is not null)
				row = row.Concat(i.Answers).ToList();
			row.AddRange(scoringGroups.Select(scoringGroup => (scores.ContainsKey((i.Id, scoringGroup.Id)) ? scores[(i.Id, scoringGroup.Id)] : 0).ToString()));
			rows.Add(row);
		}

		var content = CreateTsv(rows);
		Response.Headers.Add("content-disposition", $@"attachment;filename=""users {groupId}.tsv""");
		return Content(content, "application/octet-stream");
	}

	private async Task<List<ExtendedUserInfo>> GetExtendedUserInfo(int groupId)
	{
		var users = await groupMembersRepo.GetGroupMembersAsUsersAsync(groupId);
		var visits = await visitsRepo.FindLastVisitsInGroup(groupId);

		var result = new List<ExtendedUserInfo>();
		foreach (var user in users)
		{
			var info = new ExtendedUserInfo
			{
				Id = user.Id,
				Login = user.UserName,
				Email = user.Email,
				VkUrl = GetVk(user),
				Telegram = user.TelegramChatTitle,
				FirstName = user.FirstName,
				LastName = user.LastName,
				VisibleName = user.VisibleName,
				AvatarUrl = user.AvatarUrl,
				Gender = user.Gender
			};
			if (visits.TryGetValue(user.Id, out var visit))
			{
				info.LastVisit = visit.Timestamp;
				info.IpAddress = visit.IpAddress;
			}

			result.Add(info);
		}

		return result;
	}

	private string GetVk(ApplicationUser user)
	{
		var vkLogin = db.UserLogins.FirstOrDefault(l => l.LoginProvider == "ВКонтакте" && l.UserId == user.Id);
		return vkLogin is not null ? $"https://vk.com/id{vkLogin.ProviderKey}" : null;
	}

	private async Task<(List<string> questions, List<List<string>> values)> GetQuizAnswers(IEnumerable<string> userIds, string courseId, QuizSlide slide)
	{
		var questionBlocks = slide.Blocks.OfType<AbstractQuestionBlock>().ToList();
		var questions = questionBlocks.Select(q => q.Text.TruncateWithEllipsis(50)).ToList();
		var rows = new List<List<string>>();
		foreach (var userId in userIds)
		{
			var answers = await userQuizzesRepo.GetAnswersForShowingOnSlideAsync(courseId, slide, userId);
			var answerStrings = questionBlocks
				.Select(q
					=> answers.TryGetValue(q.Id, out var answer)
						? string.Join(",", answer.Select(i => i.ItemId ?? i.Text).Where(t => t is not null))
						: "").ToList();
			rows.Add(answerStrings);
		}

		return (questions, rows);
	}

	private Dictionary<(string UserId, string ScoringGroup), int> GetScoresByScoringGroups(List<string> userIds, List<Guid> slides, Course course)
	{
		var filterOptions = new VisitsFilterOptions
		{
			CourseId = course.Id,
			UserIds = userIds,
			SlidesIds = slides,
			PeriodStart = DateTime.MinValue,
			PeriodFinish = DateTime.MaxValue
		};
		return visitsRepo.GetVisitsInPeriod(filterOptions)
			.Select(v => new { v.UserId, v.SlideId, v.Score })
			.AsEnumerable()
			.Where(v => slides.Contains(v.SlideId))
			.GroupBy(v => (v.UserId, course.FindSlideByIdNotSafe(v.SlideId)?.ScoringGroup))
			.ToDictionary(g => g.Key, g => g.Sum(v => v.Score));
	}

	private static string CreateTsv(List<List<string>> table)
	{
		var sb = new StringBuilder();
		foreach (var row in table)
		{
			var isFirst = true;
			foreach (var cell in row)
			{
				if (!isFirst)
					sb.Append('\t');
				sb.Append(cell?.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' '));
				isFirst = false;
			}

			sb.AppendLine();
		}

		return sb.ToString();
	}

	private class ExtendedUserInfo : ShortUserInfo
	{
		public List<string> Answers;
		public string IpAddress;
		public DateTime LastVisit;
		public string Telegram;
		public string VkUrl;
	}
}