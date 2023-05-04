using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Models.Responses.Review;
using Ulearn.Web.Api.Utils;

namespace Ulearn.Web.Api.Controllers.Review;

[Authorize(Policy = "Instructors")]
[Route("review-queue/{courseId}")]
public class ReviewQueueController : BaseController
{
	private readonly ControllerUtils controllerUtils;
	private readonly ISlideCheckingsRepo slideCheckingsRepo;

	public ReviewQueueController(
		ICourseStorage courseStorage,
		UlearnDb db,
		IUsersRepo usersRepo,
		ControllerUtils controllerUtils,
		ISlideCheckingsRepo slideCheckingsRepo)
		: base(courseStorage, db, usersRepo)
	{
		this.slideCheckingsRepo = slideCheckingsRepo;
		this.controllerUtils = controllerUtils;
	}

	public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		var courseId = (string)context.ActionArguments["courseId"];

		if (!courseStorage.HasCourse(courseId))
		{
			context.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
			context.Result = new JsonResult(new ErrorResponse($"Course {courseId} not found"));
			return;
		}

		await base.OnActionExecutionAsync(context, next);
	}

	[HttpGet]
	public async Task<ReviewQueueResponse> GetReviewQueueInfo(
		[FromRoute] string courseId,
		[FromQuery] string groupsIds = null,
		[FromQuery] bool done = false,
		[FromQuery] string userId = "",
		[FromQuery] Guid? slideId = null
	)
	{
		const int maxShownQueueSize = 500;
		var groupsIdsList = groupsIds is not null ? groupsIds.Split(',').ToList() : new List<string>();
		var filterOptions = await GetManualCheckingFilterOptionsByGroup(courseId, groupsIdsList);

		if (!string.IsNullOrEmpty(userId))
			filterOptions.UserIds = new List<string> { userId };
		if (slideId.HasValue)
			filterOptions.SlidesIds = new List<Guid> { slideId.Value };

		filterOptions.Count = maxShownQueueSize + 1;
		filterOptions.OnlyChecked = done;

		var checkings = await GetMergedCheckingQueue(filterOptions);

		return ReviewQueueResponse.Build(checkings);
	}

	[HttpPost("{submissionId:int}")]
	public async Task<ActionResult> LockSubmission([FromRoute] string courseId, [FromRoute] int submissionId, [FromQuery] QueueItemType type)
	{
		AbstractManualSlideChecking checking =
			type == QueueItemType.Exercise
				? await slideCheckingsRepo.FindManualCheckingById<ManualExerciseChecking>(submissionId)
				: await slideCheckingsRepo.FindManualCheckingById<ManualQuizChecking>(submissionId);

		if (checking is null)
			return NotFound(new ErrorResponse($"Submission {submissionId} not found"));

		await slideCheckingsRepo.LockManualChecking(checking, UserId);
		return Ok($"Submission {submissionId} locked by you for 30 minutes");
	}

	private async Task<ManualCheckingQueueFilterOptions> GetManualCheckingFilterOptionsByGroup(string courseId, List<string> groupsIds)
	{
		return await controllerUtils
			.GetFilterOptionsByGroup<ManualCheckingQueueFilterOptions>(UserId, courseId, groupsIds);
	}

	private async Task<List<AbstractManualSlideChecking>> GetMergedCheckingQueue(ManualCheckingQueueFilterOptions filterOptions)
	{
		var result = (await slideCheckingsRepo
				.GetManualCheckingQueue<ManualExerciseChecking>(filterOptions))
			.Cast<AbstractManualSlideChecking>()
			.ToList();

		result.AddRange(await slideCheckingsRepo.GetManualCheckingQueue<ManualQuizChecking>(filterOptions));

		result = result
			.OrderByDescending(c => c.Timestamp)
			.ToList();
		if (filterOptions.Count > 0)
			result = result
				.Take(filterOptions.Count)
				.ToList();

		return result;
	}
}