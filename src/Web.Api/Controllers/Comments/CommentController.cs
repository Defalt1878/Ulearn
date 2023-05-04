using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Models.Comments;
using Database.Repos;
using Database.Repos.Comments;
using Database.Repos.Groups;
using Database.Repos.Users;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Swashbuckle.AspNetCore.Annotations;
using Ulearn.Common;
using Ulearn.Common.Api;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Web.Api.Models.Parameters.Comments;
using Ulearn.Web.Api.Models.Responses.Comments;

namespace Ulearn.Web.Api.Controllers.Comments
{
	[ProducesResponseType((int)HttpStatusCode.OK)]
	[SwaggerResponse((int)HttpStatusCode.Forbidden, "You don't have access to this comment")]
	[Route("/comments/{commentId:int:min(0)}")]
	public class CommentController : BaseCommentController
	{
		public CommentController(ICourseStorage courseStorage, UlearnDb db,
			IUsersRepo usersRepo, ICommentsRepo commentsRepo, ICommentLikesRepo commentLikesRepo, ICoursesRepo coursesRepo, ICourseRolesRepo courseRolesRepo,
			IAdditionalContentPublicationsRepo additionalContentPublicationsRepo,
			INotificationsRepo notificationsRepo, IGroupMembersRepo groupMembersRepo, IGroupAccessesRepo groupAccessesRepo, IVisitsRepo visitsRepo, IUnitsRepo unitsRepo)
			: base(courseStorage, db, usersRepo, commentsRepo, commentLikesRepo, coursesRepo, courseRolesRepo, additionalContentPublicationsRepo, notificationsRepo, groupMembersRepo, groupAccessesRepo, visitsRepo, unitsRepo)
		{
		}

		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			var commentId = (int)context.ActionArguments["commentId"];
			var comment = await commentsRepo.FindCommentByIdAsync(commentId, true).ConfigureAwait(false);

			var isPatchRequest = context.HttpContext.Request.Method.Equals("PATCH", StringComparison.InvariantCultureIgnoreCase);
			if (comment is null || (comment.IsDeleted && !isPatchRequest))
			{
				context.Result = NotFound(new ErrorResponse($"Comment {commentId} not found"));
				return;
			}

			if (!comment.IsApproved)
			{
				var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
				var canUserSeeNotApprovedComments = await CanUserSeeNotApprovedCommentsAsync(UserId, comment.CourseId).ConfigureAwait(false);
				if (!isAuthenticated || (!canUserSeeNotApprovedComments && comment.AuthorId != UserId))
				{
					context.Result = StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You have no access to this comment"));
					return;
				}
			}

			if (comment.IsForInstructorsOnly)
			{
				var isInstructor = await courseRolesRepo.HasUserAccessToCourse(UserId, comment.CourseId, CourseRoleType.Instructor).ConfigureAwait(false);
				if (!isInstructor)
				{
					context.Result = StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You have no access to this comment"));
					return;
				}
			}

			context.ActionArguments["comment"] = comment;

			await base.OnActionExecutionAsync(context, next).ConfigureAwait(false);
		}

		/// <summary>
		///     Информация о комментарии
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<CommentResponse>> Comment(int commentId, [FromQuery] CommentParameters parameters)
		{
			var comment = await commentsRepo.FindCommentByIdAsync(commentId).ConfigureAwait(false);
			if (comment is null)
				return NotFound(new ErrorResponse($"Comment {commentId} not found"));
			var canUserSeeNotApprovedComments = await CanUserSeeNotApprovedCommentsAsync(UserId, comment.CourseId).ConfigureAwait(false);

			var course = courseStorage.FindCourse(comment.CourseId);
			var visibleUnitsIds = await unitsRepo.GetVisibleUnitIds(course, UserId);
			var isInstructor = await courseRolesRepo.HasUserAccessToCourse(User.GetUserId(), comment.CourseId, CourseRoleType.Instructor).ConfigureAwait(false);
			var slide = course.FindSlideById(comment.SlideId, isInstructor, visibleUnitsIds);
			if (slide is null)
				return NotFound(new ErrorResponse($"Slide with comment {commentId} not found"));
			var isTester = isInstructor || await courseRolesRepo.HasUserAccessToCourse(UserId, comment.CourseId, CourseRoleType.Tester).ConfigureAwait(false);
			if (!isTester && !await additionalContentPublicationsRepo.IsSlidePublishedForUser(comment.CourseId, slide, UserId))
				return NotFound(new ErrorResponse($"Slide with id {slide.Id}"));
			var slideIsExercise = slide is ExerciseSlide;

			DefaultDictionary<int, int> likesCount;
			var likedByUserCommentsIds = (await commentLikesRepo.GetCommentsLikedByUserAsync(comment.CourseId, comment.SlideId, UserId).ConfigureAwait(false)).ToHashSet();
			var canViewAllGroupMembers = false;
			HashSet<int> userAvailableGroupsIds = null;
			if (isInstructor)
			{
				canViewAllGroupMembers = await groupAccessesRepo.CanUserSeeAllCourseGroupsAsync(User.GetUserId(), comment.CourseId).ConfigureAwait(false);
				userAvailableGroupsIds = (await groupAccessesRepo.GetAvailableForUserGroupsAsync(User.GetUserId(), false, true, true).ConfigureAwait(false)).Select(g => g.Id).ToHashSet();
			}

			if (parameters.WithReplies)
			{
				var replies = await commentsRepo.GetRepliesAsync(commentId).ConfigureAwait(false);
				var allComments = replies.Append(comment).ToList();
				likesCount = await commentLikesRepo.GetLikesCountsAsync(replies.Append(comment).Select(c => c.Id)).ConfigureAwait(false);
				var authorsIds = allComments.Select(c => c.Author.Id).Distinct().ToList();
				var authors2Groups = !isInstructor ? null : await groupMembersRepo.GetUsersGroupsAsync(comment.CourseId, authorsIds, true).ConfigureAwait(false);
				var passedSlideAuthorsIds = !slideIsExercise ? null : await visitsRepo.GetUserIdsWithPassedSlide(comment.CourseId, comment.SlideId, authorsIds);
				return BuildCommentResponse(
					comment,
					canUserSeeNotApprovedComments, new DefaultDictionary<int, List<Comment>> { { commentId, replies } }, likesCount, likedByUserCommentsIds,
					authors2Groups, passedSlideAuthorsIds, userAvailableGroupsIds, canViewAllGroupMembers, true, true, true
				);
			}

			likesCount = await commentLikesRepo.GetLikesCountsAsync(new[] { commentId }).ConfigureAwait(false);
			var author2Groups = !isInstructor ? null : new Dictionary<string, List<SingleGroup>>
			{
				{
					comment.Author.Id,
					await groupMembersRepo.GetUserGroupsAsync(comment.CourseId, comment.Author.Id).ConfigureAwait(false)
				}
			};

			var passed = slideIsExercise && await visitsRepo.IsPassed(comment.CourseId, comment.SlideId, comment.AuthorId);

			return BuildCommentResponse(
				comment,
				canUserSeeNotApprovedComments, new DefaultDictionary<int, List<Comment>>(), likesCount, likedByUserCommentsIds,
				author2Groups, passed ? new HashSet<string> { comment.AuthorId } : null,
				userAvailableGroupsIds, canViewAllGroupMembers, true, true, false
			);
		}

		/// <summary>
		///     Удалить комментарий
		/// </summary>
		[Authorize]
		[HttpDelete]
		public async Task<IActionResult> DeleteComment(int commentId)
		{
			var comment = await commentsRepo.FindCommentByIdAsync(commentId).ConfigureAwait(false);

			if (comment is null)
				return NotFound(new ErrorResponse($"Comment {commentId} not found"));

			var parentComment = comment.ParentCommentId == -1 ? null : await commentsRepo.FindCommentByIdAsync(comment.ParentCommentId).ConfigureAwait(false);
			var parentCommentCourseId = parentComment?.CourseId;

			var canEditOrDeleteComment = await CanEditOrDeleteCommentAsync(comment, UserId, parentCommentCourseId).ConfigureAwait(false);
			if (!canEditOrDeleteComment)
				return StatusCode((int)HttpStatusCode.Forbidden, "You can not delete this comment. Only author, course admin or user with special privileges can do it.");

			await commentsRepo.DeleteCommentAsync(commentId).ConfigureAwait(false);

			return Ok(new SuccessResponseWithMessage($"Comment {commentId} successfully deleted"));
		}

		/// <summary>
		///     Обновить комментарий и мета-информацию о нём. Если комментарий был удалён, то любой такой запрос восстанавливает
		///     его
		/// </summary>
		[HttpPatch]
		[Authorize]
		[SwaggerResponse((int)HttpStatusCode.RequestEntityTooLarge, "Your comment is too large")]
		public async Task<ActionResult<CommentResponse>> UpdateComment(int commentId, [FromBody] UpdateCommentParameters parameters)
		{
			var comment = await commentsRepo.FindCommentByIdAsync(commentId, true).ConfigureAwait(false);

			if (comment is null)
				return NotFound(new ErrorResponse($"Comment {commentId} not found"));

			var parentComment = comment.ParentCommentId == -1 ? null : await commentsRepo.FindCommentByIdAsync(comment.ParentCommentId).ConfigureAwait(false);
			var parentCommentCourseId = parentComment?.CourseId;

			var canEditOrDeleteComment = await CanEditOrDeleteCommentAsync(comment, UserId, parentCommentCourseId).ConfigureAwait(false);
			if (!canEditOrDeleteComment)
				return StatusCode((int)HttpStatusCode.Forbidden, "You can not delete this comment. Only author, course admin or user with special privileges can do it.");

			if (comment.IsDeleted && await CanEditOrDeleteCommentAsync(comment, UserId, parentCommentCourseId).ConfigureAwait(false))
				await commentsRepo.RestoreCommentAsync(commentId).ConfigureAwait(false);

			parameters.Text?.TrimEnd();
			if (!string.IsNullOrEmpty(parameters.Text))
				await UpdateCommentTextAsync(comment, parameters.Text, parentCommentCourseId).ConfigureAwait(false);

			if (parameters.IsApproved.HasValue)
				await UpdateCommentIsApprovedAsync(comment, parameters.IsApproved.Value, parentCommentCourseId).ConfigureAwait(false);

			if (parameters.IsPinnedToTop.HasValue)
				await UpdateCommentIsPinnedAsync(comment, parameters.IsPinnedToTop.Value, parentCommentCourseId).ConfigureAwait(false);

			if (parameters.IsCorrectAnswer.HasValue)
				await UpdateCommentIsCorrectAnswerAsync(comment, parameters.IsCorrectAnswer.Value, parentCommentCourseId).ConfigureAwait(false);

			return await Comment(commentId, new CommentParameters { WithReplies = false }).ConfigureAwait(false);
		}

		private async Task UpdateCommentTextAsync([NotNull] Comment comment, string text, string parentCommentCourseId)
		{
			var canEditOrDeleteComment = await CanEditOrDeleteCommentAsync(comment, UserId, parentCommentCourseId).ConfigureAwait(false);
			if (!canEditOrDeleteComment)
				throw new StatusCodeException((int)HttpStatusCode.Forbidden, "You can not edit this comment. Only author, course admin or user with special privileges can do it.");

			if (text.Length > CommentsPolicy.MaxCommentLength)
				throw new StatusCodeException((int)HttpStatusCode.RequestEntityTooLarge, $"Your comment is too large. Max allowed length is {CommentsPolicy.MaxCommentLength} chars");

			await commentsRepo.EditCommentTextAsync(comment.Id, text).ConfigureAwait(false);
		}

		private async Task UpdateCommentIsApprovedAsync([NotNull] Comment comment, bool isApproved, string parentCommentCourseId)
		{
			var canModerateComments =
				await CanModerateCommentsInCourseAsync(comment.CourseId, UserId).ConfigureAwait(false)
				|| (parentCommentCourseId != comment.CourseId && await CanModerateCommentsInCourseAsync(parentCommentCourseId, UserId).ConfigureAwait(false));
			if (!canModerateComments)
				throw new StatusCodeException((int)HttpStatusCode.Forbidden, "You can not approve/disapprove this comment. Only course admin or user with special privileges can do it.");

			await commentsRepo.ApproveCommentAsync(comment.Id, isApproved).ConfigureAwait(false);
			if (!comment.IsApproved && isApproved)
				await NotifyAboutNewCommentAsync(comment).ConfigureAwait(false);
		}

		private async Task UpdateCommentIsPinnedAsync([NotNull] Comment comment, bool isPinned, string parentCommentCourseId)
		{
			var canModerateComments =
				await CanModerateCommentsInCourseAsync(comment.CourseId, UserId).ConfigureAwait(false)
				|| (parentCommentCourseId != comment.CourseId && await CanModerateCommentsInCourseAsync(parentCommentCourseId, UserId).ConfigureAwait(false));
			if (!canModerateComments)
				throw new StatusCodeException((int)HttpStatusCode.Forbidden, "You can not pin/unpin this comment. Only course admin or user with special privileges can do it.");

			await commentsRepo.PinCommentAsync(comment.Id, isPinned).ConfigureAwait(false);
		}

		private async Task UpdateCommentIsCorrectAnswerAsync([NotNull] Comment comment, bool isCorrectAnswer, string parentCommentCourseId)
		{
			var canModerateComments =
				await CanModerateCommentsInCourseAsync(comment.CourseId, UserId).ConfigureAwait(false)
				|| (parentCommentCourseId != comment.CourseId && await CanModerateCommentsInCourseAsync(parentCommentCourseId, UserId).ConfigureAwait(false));
			if (!canModerateComments)
				throw new StatusCodeException((int)HttpStatusCode.Forbidden, "You can not mark this comment as correct answer or remove this mark. Only course admin or user with special privileges can do it.");

			await commentsRepo.MarkCommentAsCorrectAnswerAsync(comment.Id, isCorrectAnswer).ConfigureAwait(false);
		}

		private async Task<bool> CanEditOrDeleteCommentAsync(Comment comment, string userId, string parentCommentCourseId)
		{
			return comment.AuthorId == userId
					|| await CanModerateCommentsInCourseAsync(comment.CourseId, userId).ConfigureAwait(false)
					// ULEARN-467
					|| (parentCommentCourseId != comment.CourseId && await CanModerateCommentsInCourseAsync(parentCommentCourseId, userId).ConfigureAwait(false));
		}

		private async Task<bool> CanModerateCommentsInCourseAsync(string courseId, string userId)
		{
			var hasCourseAccessForCommentEditing = await coursesRepo.HasCourseAccess(userId, courseId, CourseAccessType.EditPinAndRemoveComments).ConfigureAwait(false);
			var isCourseAdmin = await courseRolesRepo.HasUserAccessToCourse(userId, courseId, CourseRoleType.CourseAdmin).ConfigureAwait(false);
			return hasCourseAccessForCommentEditing || isCourseAdmin;
		}

		/// <summary>
		///     Лайки к комментарию
		/// </summary>
		[HttpGet("likes")]
		public async Task<ActionResult<CommentLikesResponse>> Likes(int commentId, [FromQuery] CommentLikesParameters parameters)
		{
			var likes = await commentLikesRepo.GetLikesAsync(commentId).ConfigureAwait(false);
			var paginatedLikes = likes.Skip(parameters.Offset).Take(parameters.Count).ToList();
			return new CommentLikesResponse
			{
				Likes = paginatedLikes.Select(like => new CommentLikeInfo
				{
					User = BuildShortUserInfo(like.User),
					Timestamp = like.Timestamp
				}).ToList(),
				Pagination = new PaginationResponse
				{
					Offset = parameters.Offset,
					Count = paginatedLikes.Count,
					TotalCount = likes.Count
				}
			};
		}

		/// <summary>
		///     Лайкнуть комментарий
		/// </summary>
		[HttpPost("like")]
		[Authorize]
		public async Task<IActionResult> Like(int commentId)
		{
			if (await commentLikesRepo.DidUserLikeComment(commentId, UserId).ConfigureAwait(false))
				return Ok(new SuccessResponseWithMessage($"You have liked the comment {commentId} already"));

			await commentLikesRepo.LikeAsync(commentId, UserId).ConfigureAwait(false);

			await NotifyAboutLikedComment(commentId).ConfigureAwait(false);

			return Ok(new SuccessResponseWithMessage($"You have liked the comment {commentId}"));
		}

		/// <summary>
		///     Удалить лайк к комментарию
		/// </summary>
		[HttpDelete("like")]
		[Authorize]
		public async Task<IActionResult> Unlike(int commentId)
		{
			if (!await commentLikesRepo.DidUserLikeComment(commentId, UserId).ConfigureAwait(false))
				return Ok(new SuccessResponseWithMessage($"You don't have like for the comment {commentId}"));

			await commentLikesRepo.UnlikeAsync(commentId, UserId).ConfigureAwait(false);

			return Ok(new SuccessResponseWithMessage($"You have unliked the comment {commentId}"));
		}

		private async Task NotifyAboutLikedComment(int commentId)
		{
			var comment = await commentsRepo.FindCommentByIdAsync(commentId).ConfigureAwait(false);
			if (comment is not null)
			{
				var notification = new LikedYourCommentNotification
				{
					Comment = comment,
					LikedUserId = UserId
				};
				await notificationsRepo.AddNotification(comment.CourseId, notification, UserId).ConfigureAwait(false);
			}
		}
	}
}