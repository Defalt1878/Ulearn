using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Database;
using Database.Extensions;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Models.Parameters.Review;
using Ulearn.Web.Api.Models.Responses.Review;

namespace Ulearn.Web.Api.Controllers.Review
{
	[Route("reviews/{reviewId}/comments")]
	public class ReviewCommentsController : BaseController
	{
		private readonly ICourseRolesRepo courseRolesRepo;
		private readonly IGroupAccessesRepo groupAccessesRepo;
		private readonly INotificationsRepo notificationsRepo;
		private readonly ISlideCheckingsRepo slideCheckingsRepo;

		public ReviewCommentsController(ICourseStorage courseStorage, UlearnDb db, IUsersRepo usersRepo, IGroupAccessesRepo groupAccessesRepo,
			ISlideCheckingsRepo slideCheckingsRepo, ICourseRolesRepo courseRolesRepo, INotificationsRepo notificationsRepo)
			: base(courseStorage, db, usersRepo)
		{
			this.slideCheckingsRepo = slideCheckingsRepo;
			this.courseRolesRepo = courseRolesRepo;
			this.notificationsRepo = notificationsRepo;
			this.groupAccessesRepo = groupAccessesRepo;
		}

		/// <summary>
		///     Добавить комментарий к сделанному преподавателем ревью (т.е. замечанию к коду)
		/// </summary>
		[HttpPost]
		[Authorize]
		[SwaggerResponse((int)HttpStatusCode.Forbidden, "You don't have access to this comment")]
		[SwaggerResponse((int)HttpStatusCode.TooManyRequests, "You are commenting too fast. Please wait some time")]
		[SwaggerResponse((int)HttpStatusCode.RequestEntityTooLarge, "Your comment is too large")]
		public async Task<ActionResult<ReviewCommentResponse>> AddExerciseCodeReviewComment([FromRoute] int reviewId, [FromBody] ReviewCreateCommentParameters parameters)
		{
			var review = await slideCheckingsRepo.FindExerciseCodeReviewById(reviewId);

			var submissionUserId = review.SubmissionAuthorId;
			var submissionCourseId = review.CourseId;
			var isInstructor = await courseRolesRepo.HasUserAccessToCourse(UserId, submissionCourseId, CourseRoleType.Instructor)
								&& await groupAccessesRepo.HasInstructorEditAccessToStudentGroup(UserId, submissionUserId);
			if (submissionUserId != UserId && !isInstructor)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You can't comment this review"));

			var canReply = isInstructor || !review.Author.IsUlearnBot() || review.NotDeletedComments.Any(c => !c.Author.IsUlearnBot());
			if (!canReply)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You can't reply this review"));

			var comment = await slideCheckingsRepo.AddExerciseCodeReviewComment(UserId, reviewId, parameters.Text);

			if (review.ExerciseChecking is not { IsChecked: true })
				return ReviewCommentResponse.Build(comment);
		
			var course = courseStorage.FindCourse(submissionCourseId);
			var slideId = review.SlideId;
			if (course.FindSlideById(slideId, true, null) is not null)
				await NotifyAboutCodeReviewComment(comment);

			return ReviewCommentResponse.Build(comment);
		}

		/// <summary>
		///     Изменить содержание ревью
		/// </summary>
		[HttpPatch("{commentId:int}")]
		[Authorize]
		public async Task<ActionResult<ReviewCommentResponse>> EditReviewComment([FromRoute] int commentId, [FromBody] ReviewCreateCommentParameters parameters)
		{
			var comment = await slideCheckingsRepo.FindExerciseCodeReviewCommentById(commentId);
			if (comment is null)
				return NotFound(new ErrorResponse($"Comment {commentId} not found"));

			var courseId = comment.Review.CourseId;
			if (comment.AuthorId != UserId && !await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.CourseAdmin))
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You can't delete this comment"));

			await slideCheckingsRepo.EditExerciseCodeReviewComment(comment, parameters.Text);

			return ReviewCommentResponse.Build(comment);
		}

		/// <summary>
		///     Удалить комментарий к ревью
		/// </summary>
		[HttpDelete("{commentId:int:min(0)}")]
		[Authorize]
		[SwaggerResponse((int)HttpStatusCode.Forbidden, "You don't have access to this comment")]
		public async Task<ActionResult> DeleteExerciseCodeReviewComment([FromRoute] int reviewId, [FromRoute] int commentId)
		{
			var comment = await slideCheckingsRepo.FindExerciseCodeReviewCommentById(commentId);
			if (comment is null)
				return NotFound(new ErrorResponse($"Comment {commentId} not found"));

			var courseId = comment.Review.CourseId;
			if (comment.AuthorId != UserId && !await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.CourseAdmin))
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You can't delete this comment"));

			await slideCheckingsRepo.DeleteExerciseCodeReviewComment(comment).ConfigureAwait(false);

			return Ok(new SuccessResponseWithMessage($"Review comment {commentId} successfully deleted"));
		}

		// Оповещает о создании комментария к ревью, а не самого ревью (т.е. замечания к коду)
		// Перед вызовом этого метода нужно проверить, что посылка уже проверена, чтобы не отправить сообщение раньше.
		private async Task NotifyAboutCodeReviewComment(ExerciseCodeReviewComment comment)
		{
			var courseId = comment.Review.CourseId;
			await notificationsRepo.AddNotification(courseId, new ReceivedCommentToCodeReviewNotification
			{
				CommentId = comment.Id
			}, comment.AuthorId);
		}
	}
}