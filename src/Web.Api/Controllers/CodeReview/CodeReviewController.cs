using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Extensions;
using Ulearn.Web.Api.Models.Parameters.CodeReview;
using Ulearn.Web.Api.Models.Responses.CodeReview;

namespace Ulearn.Web.Api.Controllers.CodeReview;

[Route("/code-review")]
public class CodeReviewController : BaseController
{
	private readonly ISlideCheckingsRepo slideCheckingsRepo;
	private readonly IGroupsRepo groupsRepo;
	private readonly IUserSolutionsRepo userSolutionsRepo;
	private readonly ICourseRolesRepo courseRolesRepo;
	private readonly IGroupAccessesRepo groupAccessesRepo;

	public CodeReviewController(
		ISlideCheckingsRepo slideCheckingsRepo,
		IGroupsRepo groupsRepo,
		IUserSolutionsRepo userSolutionsRepo,
		ICourseRolesRepo courseRolesRepo,
		IGroupAccessesRepo groupAccessesRepo,
		ICourseStorage courseStorage, UlearnDb db, IUsersRepo usersRepo)
		: base(courseStorage, db, usersRepo)
	{
		this.slideCheckingsRepo = slideCheckingsRepo;
		this.groupsRepo = groupsRepo;
		this.userSolutionsRepo = userSolutionsRepo;
		this.courseRolesRepo = courseRolesRepo;
		this.groupAccessesRepo = groupAccessesRepo;
	}

	[HttpGet("{courseId}")]
	[Authorize(Policy = "Instructors")]
	[SwaggerResponse((int)HttpStatusCode.NotFound, "Course, group or user not found")]
	public async Task<ActionResult<CodeReviewSubmissionsResponse>> GetSubmissionsReviewQueue(
		string courseId,
		[FromQuery] CodeReviewFilterParameters parameters
	)
	{
		var course = courseStorage.FindCourse(courseId);
		if (course is null)
			return StatusCode((int)HttpStatusCode.NotFound, "Course not found.");

		var isSystemAdministrator = await IsSystemAdministratorAsync();
		var isCourseAdmin = isSystemAdministrator ||
							await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.CourseAdmin);

		if (parameters.StudentsFilter is StudentsFilter.All && !isCourseAdmin)
			parameters.StudentsFilter = StudentsFilter.MyGroups;

		if (parameters.StudentsFilter is StudentsFilter.GroupIds)
		{
			parameters.GroupIds ??= new List<int>();
			foreach (var groupId in parameters.GroupIds)
				if (
					await groupsRepo.IsGroupExist<SingleGroup>(groupId) ||
					(!isCourseAdmin && !await groupAccessesRepo.HasUserGrantedAccessToGroupOrIsOwnerAsync(groupId, UserId))
				)
					return StatusCode((int)HttpStatusCode.NotFound, $"Group with id {groupId} not found.");
		}

		if (parameters.StudentsFilter is StudentsFilter.StudentIds)
		{
			parameters.StudentIds ??= new List<string>();
			var accessibleStudents = isCourseAdmin || parameters.StudentIds.Count == 0
				? null
				: await groupsRepo.GetMyGroupsUsersIdsFilterAccessibleToUserAsync(courseId, UserId);
			foreach (var studentId in parameters.StudentIds)
				if (
					await usersRepo.IsUserExist(studentId) ||
					(!isCourseAdmin && !accessibleStudents!.Contains(studentId))
				)
					return StatusCode((int)HttpStatusCode.NotFound, $"Student with id {studentId} not found.");
		}

		var filterOptions = await BuildQueryFilterOptions(courseId, parameters);
		var checkings = await GetMergedCheckingQueue(filterOptions);

		var reviews = parameters.IsReviewed
			? await BuildReviewsInfo(checkings
				.OfType<ManualExerciseChecking>()
				.Select(c => c.Id)
				.ToList()
			)
			: null;

		return new CodeReviewSubmissionsResponse
		{
			Submissions = checkings
				.Select(checking => BuildSubmissionInfo(
					course,
					checking,
					reviews?.TryGetValue(checking.Id, out var checkingReviews) is true ? checkingReviews : null
				))
				.ToList()
		};
	}

	private async Task<ManualCheckingQueueFilterOptions> BuildQueryFilterOptions(string courseId, CodeReviewFilterParameters parameters)
	{
		var studentIds = parameters.StudentsFilter switch
		{
			StudentsFilter.All => null,
			StudentsFilter.StudentIds => parameters.StudentIds,
			StudentsFilter.GroupIds => await groupsRepo.GetGroupsMembersAsUserIds(parameters.GroupIds),
			StudentsFilter.MyGroups => await groupsRepo.GetMyGroupsUsersIdsFilterAccessibleToUserAsync(courseId, UserId),
			_ => null
		};
		return new ManualCheckingQueueFilterOptions
		{
			CourseId = courseId,
			UserIds = studentIds,
			SlideIds = parameters.SlideIds,
			IsReviewed = parameters.IsReviewed,
			Count = parameters.Count,
			DateSort = parameters.DateSort
		};
	}

	private async Task<List<AbstractManualSlideChecking>> GetMergedCheckingQueue(ManualCheckingQueueFilterOptions filterOptions)
	{
		var exercises = await slideCheckingsRepo.GetManualCheckingQueue<ManualExerciseChecking>(filterOptions);
		var quizzes = await slideCheckingsRepo.GetManualCheckingQueue<ManualQuizChecking>(filterOptions);

		var result = exercises
			.Cast<AbstractManualSlideChecking>()
			.Concat(quizzes);

		result = filterOptions.DateSort is DateSort.Ascending
			? result.OrderBy(c => c.Timestamp)
			: result.OrderByDescending(c => c.Timestamp);

		if (filterOptions.Count > 0)
			result = result.Take(filterOptions.Count);

		return result.ToList();
	}

	private async Task<Dictionary<int, List<ReviewComment>>> BuildReviewsInfo(IList<int> checkingIds)
	{
		var allReviews = await slideCheckingsRepo.GetExerciseCodeReviewForCheckings(checkingIds);
		var allSolutions = await userSolutionsRepo.GetSolutionsForSubmissions(checkingIds);
		return checkingIds
			.Select(id => (id, solution: allSolutions[id], solutionReviews: allReviews[id]))
			.Where(submissionReviewsInfo => submissionReviewsInfo.solution is not null)
			.ToDictionary(
				submissionReviewsInfo => submissionReviewsInfo.id,
				submissionReviewsInfo => submissionReviewsInfo.solutionReviews
					.Select(r => (
						review: r,
						startPos: submissionReviewsInfo.solution.FindPositionByLineAndCharacter(r.StartLine, r.StartPosition),
						finishPos: submissionReviewsInfo.solution.FindPositionByLineAndCharacter(r.FinishLine, r.FinishPosition)
					))
					.Where(reviewInfo => reviewInfo.finishPos - reviewInfo.startPos > 0)
					.OrderBy(reviewInfo => reviewInfo.startPos)
					.Select(reviewInfo => new ReviewComment
					{
						CommentId = reviewInfo.review.Id,
						Author = BuildShortUserInfo(reviewInfo.review.Author),
						Comment = reviewInfo.review.Comment,
						CodeFragment = submissionReviewsInfo.solution[reviewInfo.startPos..reviewInfo.finishPos]
					})
					.ToList()
			);
	}

	private static ReviewSubmissionInfo BuildSubmissionInfo(
		ICourse course,
		AbstractManualSlideChecking checking,
		List<ReviewComment>? reviews
	)
	{
		var slide = course.GetSlideByIdNotSafe(checking.SlideId);
		var maxScore = (slide as ExerciseSlide)?.Scoring.ScoreWithCodeReview ?? slide.MaxScore;
		var score = checking is ManualQuizChecking quizChecking
			? quizChecking.Score
			: ((ManualExerciseChecking)checking).Percent is { } percent
				? SlideCheckingsRepo.ConvertExerciseManualCheckingPercentToScore(percent, maxScore)
				: 0;
		return new ReviewSubmissionInfo
		{
			SubmissionId = checking.Id,
			CourseId = course.Id,
			SlideId = checking.SlideId,
			Timestamp = checking.Timestamp,
			User = BuildShortUserInfo(checking.User),
			Score = score,
			MaxScore = maxScore,
			LockedBy = BuildShortUserInfo(checking.LockedBy),
			LockedUntil = checking.LockedUntil,
			Reviews = reviews ?? new List<ReviewComment>()
		};
	}
}