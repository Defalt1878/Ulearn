using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Database;
using Database.Extensions;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Swashbuckle.AspNetCore.Annotations;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Models.Parameters.Review;
using Ulearn.Web.Api.Models.Responses.Exercise;

namespace Ulearn.Web.Api.Controllers.Review
{
	[Route("/reviews")]
	public class ReviewController : BaseController
	{
		private readonly IGroupAccessesRepo groupAccessesRepo;
		private readonly ISlideCheckingsRepo slideCheckingsRepo;
		private readonly IUserSolutionsRepo userSolutionsRepo;

		public ReviewController(ICourseStorage courseStorage, UlearnDb db, IUsersRepo usersRepo,
			ISlideCheckingsRepo slideCheckingsRepo,
			IUserSolutionsRepo userSolutionsRepo,
			IGroupAccessesRepo groupAccessesRepo)
			: base(courseStorage, db, usersRepo)
		{
			this.slideCheckingsRepo = slideCheckingsRepo;
			this.userSolutionsRepo = userSolutionsRepo;
			this.groupAccessesRepo = groupAccessesRepo;
		}

		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			var submissionId = (int)context.ActionArguments["submissionId"];

			var submission = await userSolutionsRepo.FindSubmissionById(submissionId);

			if (submission is null)
			{
				context.Result = NotFound(new ErrorResponse($"Submission {submissionId} not found"));
				return;
			}

			var courseId = submission.CourseId;
			var slideId = submission.SlideId;
			var userId = submission.UserId;

			if (submission.ManualChecking is null)
			{
				var lastAcceptedSubmission = userSolutionsRepo.GetAllAcceptedSubmissionsByUser(courseId, slideId, userId).OrderByDescending(s => s.Timestamp).FirstOrDefault();
				if (lastAcceptedSubmission is not null && lastAcceptedSubmission.Id != submission.Id)
					context.Result = StatusCode((int)HttpStatusCode.BadRequest,
						new
						{
							Status = "error",
							Error = "has_newest_submission",
							SubmissionId = lastAcceptedSubmission.Id,
							SubmissionDate = lastAcceptedSubmission.Timestamp
						});
				else
					context.Result = BadRequest(new ErrorResponse($"Submission {submissionId} doesn't contain manual checking"));
				return;
			}

			/* check below will return false if user is not sys admin and/or can't edit review for student submission */
			if (!await groupAccessesRepo.HasInstructorEditAccessToStudentGroup(UserId, submission.UserId))
			{
				context.Result = StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You don't have access to view this submission"));
				return;
			}

			await base.OnActionExecutionAsync(context, next);
		}

		/// <summary>
		///     Добавить ревью к решению
		/// </summary>
		[HttpPost]
		[Authorize]
		[SwaggerResponse((int)HttpStatusCode.RequestEntityTooLarge, "Your comment is too large")]
		public async Task<ActionResult<ReviewInfo>> AddReview([FromQuery] int submissionId, [FromBody] ReviewCreateParameters parameters)
		{
			var submission = await userSolutionsRepo.FindSubmissionById(submissionId);

			if (parameters.StartLine > parameters.FinishLine || (parameters.StartLine == parameters.FinishLine && parameters.StartPosition > parameters.FinishPosition))
			{
				var tmp = parameters.StartLine;
				parameters.StartLine = parameters.FinishLine;
				parameters.FinishLine = tmp;

				tmp = parameters.StartPosition;
				parameters.StartPosition = parameters.FinishPosition;
				parameters.FinishPosition = tmp;
			}

			var review = await slideCheckingsRepo.AddExerciseCodeReview(
				submission.ManualChecking,
				UserId,
				parameters.StartLine,
				parameters.StartPosition,
				parameters.FinishLine,
				parameters.FinishPosition,
				parameters.Text
			);

			return ReviewInfo.Build(review, null, false);
		}

		/// <summary>
		///     Удалить ревью
		/// </summary>
		[HttpDelete]
		[Authorize]
		public async Task<ActionResult> DeleteReview([FromQuery] int submissionId, [FromQuery] int reviewId)
		{
			var reviews = await userSolutionsRepo.FindSubmissionReviewsBySubmissionId(submissionId);
			var review = reviews.FirstOrDefault(r => r.Id == reviewId);

			if (review is null)
				return NotFound(new ErrorResponse($"Review {reviewId} not found"));

			/* instructor can't delete anyone else review, except ulearn bot */
			if (!review.Author.IsUlearnBot() && review.Author.Id != UserId)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You don't have access to delete this review"));

			await slideCheckingsRepo.DeleteExerciseCodeReview(review);

			return Ok(new SuccessResponseWithMessage($"Review {reviewId} successfully deleted"));
		}

		/// <summary>
		///     Изменить содержание ревью
		/// </summary>
		[HttpPatch]
		[Authorize]
		public async Task<ActionResult<ReviewInfo>> EditReview([FromQuery] int submissionId, [FromQuery] int reviewId, [FromBody] ReviewParameters parameters)
		{
			var reviews = await userSolutionsRepo.FindSubmissionReviewsBySubmissionId(submissionId);
			var review = reviews.FirstOrDefault(r => r.Id == reviewId);

			if (review is null)
				return NotFound(new ErrorResponse($"Review {reviewId} not found"));

			/* instructor can't edit anyone else review */
			if (review.Author.Id != UserId)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You don't have access to edit this review"));

			await slideCheckingsRepo.UpdateExerciseCodeReview(review, parameters.Text);
			var newReview = await slideCheckingsRepo.FindExerciseCodeReviewById(reviewId);

			return ReviewInfo.Build(newReview, null, false);
		}
	}
}