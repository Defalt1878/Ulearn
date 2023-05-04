using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Web.Api.Models.Parameters.Exercise;
using Ulearn.Web.Api.Models.Responses.AcceptedSolutions;

namespace Ulearn.Web.Api.Controllers.Slides;

[Route("accepted-solutions")]
public class AcceptedSolutionsController : BaseController
{
	private readonly IAcceptedSolutionsRepo acceptedSolutionsRepo;
	private readonly ICourseRolesRepo courseRolesRepo;
	private readonly IUnitsRepo unitsRepo;
	private readonly IUserSolutionsRepo userSolutionsRepo;
	private readonly IVisitsRepo visitsRepo;

	public AcceptedSolutionsController(ICourseStorage courseStorage, UlearnDb db, IUsersRepo usersRepo,
		IAcceptedSolutionsRepo acceptedSolutionsRepo, ICourseRolesRepo courseRolesRepo, IUnitsRepo unitsRepo, IVisitsRepo visitsRepo, IUserSolutionsRepo userSolutionsRepo)
		: base(courseStorage, db, usersRepo)
	{
		this.acceptedSolutionsRepo = acceptedSolutionsRepo;
		this.courseRolesRepo = courseRolesRepo;
		this.unitsRepo = unitsRepo;
		this.visitsRepo = visitsRepo;
		this.userSolutionsRepo = userSolutionsRepo;
	}

	/// <summary>
	///     Получить чужие решения
	/// </summary>
	[HttpGet]
	[Authorize]
	[SwaggerResponse((int)HttpStatusCode.PreconditionFailed, "You must solve exercise or agree to lose points")]
	[SwaggerResponse((int)HttpStatusCode.NotFound, "Course or slide are not found")]
	public async Task<ActionResult<AcceptedSolutionsResponse>> GetAcceptedSolutions(string courseId, Guid slideId)
	{
		var course = courseStorage.FindCourse(courseId);
		if (course is null)
			return NotFound(new { status = "error", message = "Course not found" });

		var isInstructor = await courseRolesRepo.HasUserAccessToCourse(UserId, course.Id, CourseRoleType.Instructor);
		var visibleUnitsIds = await unitsRepo.GetVisibleUnitIds(course, UserId);
		var slide = course.FindSlideById(slideId, isInstructor, visibleUnitsIds) as ExerciseSlide;

		if (slide is null)
			return NotFound(new { status = "error", message = "Slide not found" });

		if (slide.Exercise.HideShowSolutionsButton)
			return NotFound(new { status = "error", message = "Showing solutions is not enabled for this slide" });

		var isSkippedOrPassed = await visitsRepo.IsSkippedOrPassed(course.Id, slide.Id, UserId);
		if (!isSkippedOrPassed)
			return StatusCode((int)HttpStatusCode.NotAcceptable, new { status = "error", message = "You must solve exercise or agree to lose points" });

		var submissionsLikedByMe = await acceptedSolutionsRepo.GetSubmissionsLikedByMe(course.Id, slideId, UserId);

		var promotedSolutions = (await acceptedSolutionsRepo.GetPromotedSubmissions(course.Id, slideId))
			.Select(s => new AcceptedSolution(s.SubmissionId, s.Code, s.Language, s.LikesCount, submissionsLikedByMe.Contains(s.SubmissionId), isInstructor ? BuildShortUserInfo(s.UserWhoPromote) : null))
			.ToList();

		var randomLikedSolution = await acceptedSolutionsRepo.GetRandomLikedSubmission(course.Id, slideId);
		if (randomLikedSolution is not null && promotedSolutions.Any(s => s.SubmissionId == randomLikedSolution.Value.SubmissionId || s.Code == randomLikedSolution.Value.Code))
			randomLikedSolution = null;
		var randomLikedSolutions = Enumerable.Repeat(randomLikedSolution, 1).Where(s => s.HasValue)
			.Select(s => new AcceptedSolution(s.Value.SubmissionId, s.Value.Code, s.Value.Language, s.Value.LikesCount, submissionsLikedByMe.Contains(s.Value.SubmissionId), null))
			.ToList();

		const int newestSolutionsCount = 5;
		var newestSolutions = (await acceptedSolutionsRepo.GetNewestSubmissions(course.Id, slideId, newestSolutionsCount + promotedSolutions.Count))
			.Where(s => !promotedSolutions.Concat(randomLikedSolutions)
				.Any(existing => s.SubmissionId == existing.SubmissionId || s.Code == existing.Code))
			.Take(newestSolutionsCount)
			.Select(s => new AcceptedSolution(s.SubmissionId, s.Code, s.Language, s.LikesCount, submissionsLikedByMe.Contains(s.SubmissionId), null))
			.ToList();

		return new AcceptedSolutionsResponse
		{
			PromotedSolutions = promotedSolutions,
			RandomLikedSolutions = randomLikedSolutions,
			NewestSolutions = newestSolutions
		};
	}

	/// <summary>
	///     Получить полайканные студентами решения. Используется преподавателями
	/// </summary>
	[HttpGet("liked")]
	[Authorize(Policy = "Instructors")]
	public async Task<ActionResult<LikedAcceptedSolutionsResponse>> GetLikedAcceptedSolutions([FromQuery] LikedAcceptedSolutionsParameters p)
	{
		var submissionsLikedByMe = await acceptedSolutionsRepo.GetSubmissionsLikedByMe(p.CourseId, p.SlideId, UserId);

		var likedSolutions = (await acceptedSolutionsRepo.GetLikedAcceptedSolutions(p.CourseId, p.SlideId, p.Offset, p.Count))
			.Select(s => new AcceptedSolution(s.SubmissionId, s.Code, s.Language, s.LikesCount, submissionsLikedByMe.Contains(s.SubmissionId), null))
			.ToList();
		return new LikedAcceptedSolutionsResponse
		{
			LikedSolutions = likedSolutions
		};
	}

	/// <summary>
	///     Лайкнуть решение
	/// </summary>
	[HttpPut("like")]
	[Authorize]
	public async Task<IActionResult> Like(int solutionId)
	{
		var submission = await userSolutionsRepo.FindSubmissionByIdNoTracking(solutionId);
		var courseId = submission.CourseId;
		var slideId = submission.SlideId;

		if (await acceptedSolutionsRepo.DidUserLikeSubmission(solutionId, UserId))
			return Ok(new SuccessResponseWithMessage($"You have liked the solution {solutionId} already"));

		await acceptedSolutionsRepo.TryLikeSubmission(courseId, slideId, solutionId, UserId);

		return Ok(new SuccessResponseWithMessage($"You have liked the solution {solutionId}"));
	}

	/// <summary>
	///     Удалить лайк к решению
	/// </summary>
	[HttpDelete("like")]
	[Authorize]
	public async Task<IActionResult> Unlike(int solutionId)
	{
		if (!await acceptedSolutionsRepo.DidUserLikeSubmission(solutionId, UserId))
			return Ok(new SuccessResponseWithMessage($"You don't have like for the solution {solutionId}"));

		await acceptedSolutionsRepo.TryUnlikeSubmission(solutionId, UserId);

		return Ok(new SuccessResponseWithMessage($"You have unliked the solution {solutionId}"));
	}

	/// <summary>
	///     Рекомендовать решение от имени преподавателей курса
	/// </summary>
	[HttpPut("promote")]
	[Authorize]
	public async Task<IActionResult> Promote(int solutionId) // courseId для проверки прав
	{
		var submission = await userSolutionsRepo.FindSubmissionByIdNoTracking(solutionId);
		var courseId = submission.CourseId;
		var slideId = submission.SlideId;

		var isInstructor = await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.Instructor).ConfigureAwait(false);
		if (!isInstructor)
			return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You have no instructor access to this course"));

		if (await acceptedSolutionsRepo.HasSubmissionBeenPromoted(solutionId))
			return Ok(new SuccessResponseWithMessage($"You have promoted the solution {solutionId} already"));

		await acceptedSolutionsRepo.TryPromoteSubmission(courseId, slideId, solutionId, UserId);

		return Ok(new SuccessResponseWithMessage($"You have promoted the solution {solutionId}"));
	}

	/// <summary>
	///     Убрать решение из рекомендаций от имени преподавателей курса
	/// </summary>
	[HttpDelete("promote")]
	[Authorize]
	public async Task<IActionResult> Unpromote(int solutionId) // courseId для проверки прав
	{
		var submission = await userSolutionsRepo.FindSubmissionByIdNoTracking(solutionId);
		var courseId = submission.CourseId;

		var isInstructor = await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.Instructor).ConfigureAwait(false);
		if (!isInstructor)
			return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You have no instructor access to this course"));

		if (!await acceptedSolutionsRepo.HasSubmissionBeenPromoted(solutionId))
			return Ok(new SuccessResponseWithMessage($"You don't have promoted for the solution {solutionId}"));

		await acceptedSolutionsRepo.TryUnpromoteSubmission(solutionId, UserId);

		return Ok(new SuccessResponseWithMessage($"You have unpromoted the solution {solutionId}"));
	}
}