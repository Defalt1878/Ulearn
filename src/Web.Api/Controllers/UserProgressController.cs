using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.Users;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Web.Api.Models.Parameters;
using Ulearn.Web.Api.Models.Responses.Users;
using Ulearn.Web.Api.Utils.LTI;

namespace Ulearn.Web.Api.Controllers
{
	[Route("/user-progress/{courseId}")]
	public class UserProgressController : BaseController
	{
		private readonly IAdditionalScoresRepo additionalScoresRepo;
		private readonly ICourseRolesRepo courseRolesRepo;
		private readonly IGroupAccessesRepo groupAccessesRepo;
		private readonly IGroupMembersRepo groupMembersRepo;
		private readonly IGroupsRepo groupsRepo;
		private readonly ILtiConsumersRepo ltiConsumersRepo;
		private readonly ILtiRequestsRepo ltiRequestsRepo;
		private readonly ISlideCheckingsRepo slideCheckingsRepo;
		private readonly IUnitsRepo unitsRepo;
		private readonly IUserQuizzesRepo userQuizzesRepo;
		private readonly IVisitsRepo visitsRepo;

		public UserProgressController(ICourseStorage courseStorage, UlearnDb db, IUsersRepo usersRepo,
			IVisitsRepo visitsRepo, IUserQuizzesRepo userQuizzesRepo, IAdditionalScoresRepo additionalScoresRepo,
			ICourseRolesRepo courseRolesRepo, IGroupAccessesRepo groupAccessesRepo, IGroupMembersRepo groupMembersRepo,
			ISlideCheckingsRepo slideCheckingsRepo, ILtiRequestsRepo ltiRequestsRepo, ILtiConsumersRepo ltiConsumersRepo, IUnitsRepo unitsRepo, IGroupsRepo groupsRepo)
			: base(courseStorage, db, usersRepo)
		{
			this.visitsRepo = visitsRepo;
			this.userQuizzesRepo = userQuizzesRepo;
			this.additionalScoresRepo = additionalScoresRepo;
			this.courseRolesRepo = courseRolesRepo;
			this.groupAccessesRepo = groupAccessesRepo;
			this.groupMembersRepo = groupMembersRepo;
			this.slideCheckingsRepo = slideCheckingsRepo;
			this.ltiRequestsRepo = ltiRequestsRepo;
			this.ltiConsumersRepo = ltiConsumersRepo;
			this.unitsRepo = unitsRepo;
			this.groupsRepo = groupsRepo;
		}

		/// <summary>
		///     Прогресс пользователя в курсе
		/// </summary>
		[HttpPost]
		[Authorize]
		public async Task<ActionResult<UsersProgressResponse>> UserProgress([FromRoute] string courseId, [FromBody] UserProgressParameters parameters)
		{
			if (!courseStorage.HasCourse(courseId))
				return NotFound(new ErrorResponse($"Course {courseId} not found"));
			var course = courseStorage.FindCourse(courseId);
			var userIds = parameters.UserIds;
			if (userIds is null || userIds.Count == 0)
			{
				userIds = new List<string> { UserId };
			}
			else
			{
				var userIdsWithProgressNotVisibleForUser = await GetUserIdsWithProgressNotVisibleForUser(course.Id, userIds);
				if (userIdsWithProgressNotVisibleForUser?.Any() ?? false)
				{
					var userIdsStr = string.Join(", ", userIdsWithProgressNotVisibleForUser);
					return NotFound(new ErrorResponse($"Users {userIdsStr} not found"));
				}
			}

			HashSet<Guid> visibleSlides;
			if (userIds.Count == 1 && userIds[0] == UserId)
			{
				var isInstructor = await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.Instructor).ConfigureAwait(false);
				var visibleUnits = await unitsRepo.GetVisibleUnitIds(course, UserId);
				visibleSlides = course.GetSlides(isInstructor, visibleUnits).Select(s => s.Id).ToHashSet();
			}
			else
			{
				var publishedUnits = await unitsRepo.GetPublishedUnitIds(course);
				visibleSlides = course.GetSlides(false, publishedUnits).Select(s => s.Id).ToHashSet();
			}

			var scores = await visitsRepo.GetScoresForSlides(course.Id, userIds);
			var visitsTimestamps = await visitsRepo.GetLastVisitsInCourse(course.Id, UserId);
			var additionalScores = await GetAdditionalScores(course.Id, userIds).ConfigureAwait(false);
			var attempts = await userQuizzesRepo.GetUsedAttemptsCountAsync(course.Id, userIds).ConfigureAwait(false);
			var waitingQuizSlides = await userQuizzesRepo.GetSlideIdsWaitingForManualCheckAsync(course.Id, userIds).ConfigureAwait(false);
			var waitingExerciseSlides = await slideCheckingsRepo.GetSlideIdsWaitingForManualExerciseCheckAsync(course.Id, userIds).ConfigureAwait(false);
			var prohibitFurtherManualCheckingSlides = await slideCheckingsRepo.GetProhibitFurtherManualCheckingSlides(course.Id, userIds).ConfigureAwait(false);
			var skippedSlides = await visitsRepo.GetSkippedSlides(course.Id, userIds);
			var usersIdsWithEnabledManualChecking = await groupsRepo.GetUsersIdsWithEnabledManualChecking(course, userIds);

			var usersProgress = new Dictionary<string, UserProgress>();
			foreach (var userId in scores.Keys)
			{
				var visitedSlides
					= scores[userId]
						.Where(kvp => visibleSlides.Contains(kvp.Key))
						.ToDictionary(kvp => kvp.Key, kvp => new UserProgressSlideResult
						{
							Visited = true,
							Timestamp = visitsTimestamps.TryGetValue(kvp.Key, out var visit) ? visit.Timestamp : null,
							Score = kvp.Value,
							IsSkipped = skippedSlides.GetValueOrDefault(userId)?.Contains(kvp.Key) ?? false,
							UsedAttempts = attempts.GetValueOrDefault(userId)?.GetValueOrDefault(kvp.Key) ?? 0,
							WaitingForManualChecking =
								usersIdsWithEnabledManualChecking.Contains(userId)
								&& ((waitingExerciseSlides.GetValueOrDefault(userId)?.Contains(kvp.Key) ?? false) || (waitingQuizSlides.GetValueOrDefault(userId)?.Contains(kvp.Key) ?? false)),
							ProhibitFurtherManualChecking = prohibitFurtherManualCheckingSlides.GetValueOrDefault(userId)?.Contains(kvp.Key) ?? false
						});
				var userAdditionalScores = additionalScores.GetValueOrDefault(userId);
				usersProgress[userId] = new UserProgress
				{
					VisitedSlides = visitedSlides,
					AdditionalScores = userAdditionalScores
				};
			}

			return new UsersProgressResponse
			{
				UserProgress = usersProgress
			};
		}

		[ItemCanBeNull]
		private async Task<List<string>> GetUserIdsWithProgressNotVisibleForUser(string courseId, List<string> userIds)
		{
			var isSystemAdministrator = await IsSystemAdministratorAsync().ConfigureAwait(false);
			if (isSystemAdministrator)
				return null;
			if (await groupAccessesRepo.CanUserSeeAllCourseGroupsAsync(UserId, courseId))
				return null;
			var userRole = await courseRolesRepo.GetRole(UserId, courseId).ConfigureAwait(false);
			var groups = userRole == CourseRoleType.Instructor ? (await groupAccessesRepo.GetAvailableForUserGroupsAsync(courseId, UserId, false, true, false, GroupQueryType.SingleGroup)).AsGroups() : new List<SingleGroup>();
			groups = groups
				.Concat((await groupMembersRepo.GetUserGroupsAsync(courseId, UserId)).AsGroups().Where(g => g.CanUsersSeeGroupProgress))
				.Distinct().ToList();
			var members = new[] { UserId }.Concat(await groupMembersRepo.GetGroupsMembersIdsAsync(groups.Select(g => g.Id).ToList())).ToHashSet();
			var allIdsInMembers = members.IsSupersetOf(userIds);
			if (allIdsInMembers)
				return null;
			var notVisibleUserIds = userIds.ToHashSet();
			notVisibleUserIds.ExceptWith(members);
			return notVisibleUserIds.ToList();
		}

		private async Task<Dictionary<string, Dictionary<Guid, Dictionary<string, int>>>> GetAdditionalScores(string courseId, List<string> userIds)
		{
			return (await additionalScoresRepo.GetAdditionalScoresForUsers(courseId, userIds).ConfigureAwait(false))
				.Select(kvp => (userId: kvp.Key.Item1, unitId: kvp.Key.Item2, scoringGroupId: kvp.Key.Item3, additionalScore: kvp.Value.Score))
				.GroupBy(t => t.userId)
				.ToDictSafe(g => g.Key,
					g =>
					{
						return g.GroupBy(t => t.unitId)
							.ToDictSafe(g2 => g2.Key,
								g2 =>
									g2.ToDictSafe(t => t.scoringGroupId, t => t.additionalScore));
					});
		}

		/// <summary>
		///     Отметить посещение слайда в курсе
		/// </summary>
		/// <returns></returns>
		[HttpPost("visit/{slideId:guid}")]
		[HttpPut("{slideId:guid}/visit")]
		[Authorize]
		public async Task<ActionResult<UsersProgressResponse>> Visit([FromRoute] Course course, [FromRoute] Guid slideId)
		{
			var isInstructor = await courseRolesRepo.HasUserAccessToCourse(UserId, course.Id, CourseRoleType.Instructor).ConfigureAwait(false);
			var slide = course.FindSlideByIdNotSafe(slideId);
			if (slide is null)
			{
				var instructorNote = course.FindInstructorNoteByIdNotSafe(slideId);
				if (instructorNote is not null && isInstructor)
					slide = instructorNote;
			}

			if (slide is null)
				return StatusCode((int)HttpStatusCode.NotFound, $"No slide with id {slideId}");
			var visit = await visitsRepo.AddVisit(course.Id, slideId, UserId, GetRealClientIp());
			await ResendLtiScore(course.Id, slide, visit);
			return await UserProgress(course.Id, new UserProgressParameters());
		}

		[HttpPut("{slideId:guid}/prohibit-further-manual-checking")]
		[Authorize(Policy = "Instructors")]
		public async Task<ActionResult> ProhibitFurtherManualChecking([FromRoute] string courseId, [FromRoute] Guid slideId, [FromQuery] string userId, [FromQuery] bool prohibit)
		{
			if (!await groupAccessesRepo.HasInstructorViewAccessToStudentGroup(User.GetUserId(), userId))
				return StatusCode((int)HttpStatusCode.Forbidden, "This student should be member of one of accessible for you groups");

			if (prohibit)
				await slideCheckingsRepo.ProhibitFurtherExerciseManualChecking(courseId, userId, slideId);
			else
				await slideCheckingsRepo.EnableFurtherManualCheckings(courseId, userId, slideId);

			return Ok($"{(prohibit ? "Prohibited" : "Enabled")} further exercise manual checking  for {courseId}/{slideId}");
		}

		/// <summary>
		///     Пропустить задачу. Это позволит увидеть чужие решения. Но не даст получить баллы за задачу, если их еще нет
		/// </summary>
		[HttpPut("{slideId:guid}/exercise/skip")]
		[Authorize]
		public async Task<IActionResult> SkipExercise([FromRoute] Course course, [FromRoute] Guid slideId)
		{
			if (course is null)
				return NotFound(new { status = "error", message = "Course not found" });

			var isInstructor = await courseRolesRepo.HasUserAccessToCourse(UserId, course.Id, CourseRoleType.Instructor);
			var visibleUnitsIds = await unitsRepo.GetVisibleUnitIds(course, UserId);
			var exerciseSlide = course.FindSlideById(slideId, isInstructor, visibleUnitsIds) as ExerciseSlide;

			if (exerciseSlide is null)
				return NotFound(new { status = "error", message = "Slide not found" });

			if (exerciseSlide.Exercise.HideShowSolutionsButton)
				return NotFound(new { status = "error", message = "Showing solutions is not enabled for this slide" });

			var isSkippedOrPassed = await visitsRepo.IsSkippedOrPassed(course.Id, exerciseSlide.Id, UserId);
			if (isSkippedOrPassed)
				return Ok(new SuccessResponseWithMessage("You have already passed or skipped the slide"));

			await visitsRepo.SkipSlide(course.Id, exerciseSlide.Id, UserId);
			return Ok(new SuccessResponseWithMessage($"You have skipped slide {slideId}"));
		}

		private async Task ResendLtiScore(string courseId, Slide slide, Visit visit)
		{
			if (!visit.IsPassed)
				return;
			var ltiRequestJson = await ltiRequestsRepo.Find(courseId, UserId, slide.Id);
			if (ltiRequestJson is not null)
				await LtiUtils.SubmitScore(slide, UserId, visit.Score, ltiRequestJson, ltiConsumersRepo);
		}

		private string GetRealClientIp()
		{
			var xForwardedFor = Request.Headers["X-Forwarded-For"].ToString();
			if (string.IsNullOrEmpty(xForwardedFor))
				return Request.Host.Host;
			return xForwardedFor.Split(',').FirstOrDefault() ?? "";
		}
	}
}