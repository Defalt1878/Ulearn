using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Database;
using Database.Repos;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Swashbuckle.AspNetCore.Annotations;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Models.Responses.Review;

namespace Ulearn.Web.Api.Controllers.Review;

[Route("/favourite-reviews")]
[Authorize(Policy = "Instructors")]
public class FavouriteReviewsController : BaseController
{
	private readonly IFavouriteReviewsRepo favouriteReviewsRepo;
	private readonly ISlideCheckingsRepo slideCheckingsRepo;

	public FavouriteReviewsController(
		ICourseStorage courseStorage,
		UlearnDb db,
		IFavouriteReviewsRepo favouriteReviewsRepo,
		ISlideCheckingsRepo slideCheckingsRepo,
		IUsersRepo usersRepo)
		: base(courseStorage, db, usersRepo)
	{
		this.favouriteReviewsRepo = favouriteReviewsRepo;
		this.slideCheckingsRepo = slideCheckingsRepo;
	}

	public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		var courseId = (string)context.ActionArguments["courseId"];
		var slideId = (Guid)context.ActionArguments["slideId"];

		var course = courseStorage.FindCourse(courseId);
		if (course is null)
		{
			context.Result = NotFound(new ErrorResponse($"Course {courseId} not found"));
			return;
		}

		var slide = course.FindSlideByIdNotSafe(slideId);
		if (slide is null)
		{
			context.Result = NotFound(new ErrorResponse($"Slide with id {slideId} not found"));
			return;
		}

		await next();
	}

	[HttpGet]
	[SwaggerResponse((int)HttpStatusCode.Forbidden, "You don't have access to view submissions")]
	[SwaggerResponse((int)HttpStatusCode.NotFound, "Course {course} not found")]
	[SwaggerResponse((int)HttpStatusCode.NotFound, "Slide with id {slideId} not found")]
	public async Task<ActionResult<FavouriteReviewsResponse>> GetFavouriteReviews([FromQuery] string courseId, [FromQuery] Guid slideId)
	{
		var startDate = DateTime.Now.AddYears(-1).AddMonths(-6);

		var userFavouriteReviews = await favouriteReviewsRepo.GetFavouriteReviewsForUser(courseId, slideId, UserId);
		var favouriteReviewsForOthersUsers = await favouriteReviewsRepo.GetFavouriteReviewsForOtherUsers(courseId, slideId, UserId, startDate);
		var lastReviews = await slideCheckingsRepo.GetLastUsedExerciseCodeReviewsTexts(courseId, slideId, UserId, 5, userFavouriteReviews.Select(fr => fr.Text).ToList());

		return FavouriteReviewsResponse.Build(favouriteReviewsForOthersUsers, userFavouriteReviews, lastReviews);
	}

	[HttpPost]
	[SwaggerResponse((int)HttpStatusCode.Forbidden, "You don't have access to view submissions")]
	[SwaggerResponse((int)HttpStatusCode.NotFound, "Course {course} not found")]
	[SwaggerResponse((int)HttpStatusCode.NotFound, "Slide with id {slideId} not found")]
	public async Task<ActionResult<FavouriteReviewResponse>> AddFavouriteReview([FromQuery] string courseId, [FromQuery] Guid slideId, [FromBody] string text)
	{
		var favouriteReviewByUser = await favouriteReviewsRepo.AddFavouriteReviewByUser(courseId, slideId, UserId, text);
		return FavouriteReviewResponse.Build(favouriteReviewByUser);
	}

	[HttpDelete]
	[SwaggerResponse((int)HttpStatusCode.Forbidden, "You don't have access to view submissions")]
	[SwaggerResponse((int)HttpStatusCode.NotFound, "Course {course} not found")]
	[SwaggerResponse((int)HttpStatusCode.NotFound, "Slide with id {slideId} not found")]
	[SwaggerResponse((int)HttpStatusCode.NotFound, "Favourite review with id {favouriteReviewId} not found")]
	public async Task<ActionResult> DeleteFavouriteReviewByUser([FromQuery] string courseId, [FromQuery] Guid slideId, [FromQuery] int favouriteReviewId)
	{
		var favouriteReviewByUser = await favouriteReviewsRepo.FindFavouriteReviewByUser(courseId, slideId, UserId, favouriteReviewId);
		if (favouriteReviewByUser is null)
			return NotFound(new ErrorResponse($"Favourite review with id {favouriteReviewId} not found"));

		await favouriteReviewsRepo.DeleteFavouriteReviewByUser(favouriteReviewByUser);

		return Ok("Favourite review deleted");
	}
}