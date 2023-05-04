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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Ulearn.Common;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Web.Api.Authorization;
using Ulearn.Web.Api.Models.Parameters.Comments;
using Ulearn.Web.Api.Models.Responses.Comments;

namespace Ulearn.Web.Api.Controllers.Comments
{
	[Route("/comments")]
	public class CommentsController : BaseCommentController
	{
		private readonly ICommentPoliciesRepo commentPoliciesRepo;

		public CommentsController(ICourseStorage courseStorage, UlearnDb db,
			ICommentsRepo commentsRepo, ICommentLikesRepo commentLikesRepo, ICommentPoliciesRepo commentPoliciesRepo,
			IUsersRepo usersRepo, ICoursesRepo coursesRepo, ICourseRolesRepo courseRolesRepo, INotificationsRepo notificationsRepo,
			IAdditionalContentPublicationsRepo additionalContentPublicationsRepo,
			IGroupMembersRepo groupMembersRepo, IGroupAccessesRepo groupAccessesRepo, IVisitsRepo visitsRepo, IUnitsRepo unitsRepo)
			: base(courseStorage, db, usersRepo, commentsRepo, commentLikesRepo, coursesRepo, courseRolesRepo, additionalContentPublicationsRepo, notificationsRepo, groupMembersRepo, groupAccessesRepo, visitsRepo, unitsRepo)
		{
			this.commentPoliciesRepo = commentPoliciesRepo;
		}

		/// <summary>
		///     Комментарии под слайдом
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<CommentsListResponse>> SlideComments([FromQuery] SlideCommentsParameters parameters)
		{
			var courseId = parameters.CourseId;
			var slideId = parameters.SlideId;
			var course = courseStorage.GetCourse(courseId);
			var userId = UserId;

			var isInstructor = await courseRolesRepo.HasUserAccessToCourse(userId, courseId, CourseRoleType.Instructor).ConfigureAwait(false);
			var isTester = isInstructor || await courseRolesRepo.HasUserAccessToCourse(userId, courseId, CourseRoleType.Tester).ConfigureAwait(false);
			var visibleUnits = await unitsRepo.GetVisibleUnitIds(course, userId).ConfigureAwait(false);
			var slide = GetSlide(courseId, slideId, isInstructor, visibleUnits);
			if (slide is null)
				return StatusCode((int)HttpStatusCode.NotFound, $"No slide with id {slideId}");

			if (!isTester && !await additionalContentPublicationsRepo.IsSlidePublishedForUser(courseId, slide, userId))
				return StatusCode((int)HttpStatusCode.NotFound, $"No slide with id {slideId}");

			if (parameters.ForInstructors)
			{
				if (!IsAuthenticated)
					return StatusCode((int)HttpStatusCode.Unauthorized, "You should be authenticated to view instructor comments.");
				if (!isInstructor)
					return StatusCode((int)HttpStatusCode.Forbidden, $"You have no access to instructor comments on {courseId}. You should be instructor or course admin.");
			}

			var comments = await commentsRepo.GetSlideTopLevelCommentsAsync(courseId, slideId).ConfigureAwait(false);
			comments = comments.Where(c => c.IsForInstructorsOnly == parameters.ForInstructors).ToList();
			return await GetSlideCommentsResponseAsync(comments, courseId, parameters, slide).ConfigureAwait(false);
		}

		private Slide GetSlide(string courseId, Guid slideId, bool isInstructor, IEnumerable<Guid> visibleUnits)
		{
			var course = courseStorage.GetCourse(courseId);
			var slide = course.FindSlideById(slideId, isInstructor, visibleUnits);
			if (slide is null)
			{
				var instructorNote = course.FindInstructorNoteByIdNotSafe(slideId);
				if (instructorNote is not null && isInstructor)
					slide = instructorNote;
			}

			return slide;
		}

		private async Task<ActionResult<CommentsListResponse>> GetSlideCommentsResponseAsync(List<Comment> comments, string courseId, SlideCommentsParameters parameters, Slide slide)
		{
			var canUserSeeNotApprovedComments = await CanUserSeeNotApprovedCommentsAsync(UserId, courseId).ConfigureAwait(false);
			comments = FilterVisibleComments(comments, canUserSeeNotApprovedComments);

			var totalCount = comments.Count;
			comments = comments.Skip(parameters.Offset).Take(parameters.Count).ToList();

			var slideIsExercise = slide is ExerciseSlide;

			var replies = await commentsRepo.GetRepliesAsync(comments.Select(c => c.Id)).ConfigureAwait(false);
			var allComments = comments.Concat(replies.SelectMany(g => g.Value)).ToList();

			var commentLikesCount = await commentLikesRepo.GetLikesCountsAsync(allComments.Select(c => c.Id)).ConfigureAwait(false);
			var likedByUserCommentsIds = (await commentLikesRepo.GetCommentsLikedByUserAsync(courseId, parameters.SlideId, UserId).ConfigureAwait(false)).ToHashSet();
			var isInstructor = await courseRolesRepo.HasUserAccessToCourse(User.GetUserId(), courseId, CourseRoleType.Instructor).ConfigureAwait(false);
			var canViewAllGroupMembers = isInstructor && await groupAccessesRepo.CanUserSeeAllCourseGroupsAsync(User.GetUserId(), courseId).ConfigureAwait(false);
			var userAvailableGroupsIds = !isInstructor ? null : (await groupAccessesRepo.GetAvailableForUserGroupsAsync(User.GetUserId(), false, true, true).ConfigureAwait(false)).Select(g => g.Id).ToHashSet();
			var authorsIds = allComments.Select(c => c.Author.Id).Distinct().ToList();
			var authors2Groups = !isInstructor ? null : await groupMembersRepo.GetUsersGroupsAsync(courseId, authorsIds, true).ConfigureAwait(false);
			var passedSlideAuthorsIds = !slideIsExercise ? null : await visitsRepo.GetUserIdsWithPassedSlide(parameters.CourseId, parameters.SlideId, authorsIds);

			return new CommentsListResponse
			{
				TopLevelComments = BuildCommentsListResponse(comments, canUserSeeNotApprovedComments, replies, commentLikesCount, likedByUserCommentsIds,
					authors2Groups, passedSlideAuthorsIds, userAvailableGroupsIds, canViewAllGroupMembers,
					false, true, true),
				Pagination = new PaginationResponse
				{
					Offset = parameters.Offset,
					Count = comments.Count,
					TotalCount = totalCount
				}
			};
		}

		/// <summary>
		///     Добавить комментарий под слайдом
		/// </summary>
		[Authorize]
		[HttpPost]
		[SwaggerResponse((int)HttpStatusCode.TooManyRequests, "You are commenting too fast. Please wait some time")]
		[SwaggerResponse((int)HttpStatusCode.RequestEntityTooLarge, "Your comment is too large")]
		public async Task<ActionResult<CommentResponse>> CreateComment([FromQuery] CourseAuthorizationParameters courseAuthorizationParameters, CreateCommentParameters parameters)
		{
			var courseId = courseAuthorizationParameters.CourseId;
			var slideId = parameters.SlideId;
			var userId = UserId;
			parameters.Text.TrimEnd();

			var course = courseStorage.GetCourse(courseId);
			var isInstructor = await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.Instructor).ConfigureAwait(false);
			var isTester = isInstructor || await courseRolesRepo.HasUserAccessToCourse(userId, courseId, CourseRoleType.Tester).ConfigureAwait(false);
			var visibleUnits = await unitsRepo.GetVisibleUnitIds(course, UserId).ConfigureAwait(false);
			var slide = GetSlide(courseId, slideId, isInstructor, visibleUnits);
			if (slide is null)
				return StatusCode((int)HttpStatusCode.NotFound, $"No slide with id {slideId}");

			if (!isTester && (slide.IsExtraContent || slide.Unit.Settings.IsExtraContent))
			{
				var userGroups = await groupMembersRepo.GetUserGroupsAsync(course.Id, userId);

				if (userGroups.Count == 0)
					return StatusCode((int)HttpStatusCode.Forbidden, $"You have no access to view comments on slide with id {slideId}. You are not in any groups.");

				var isSlidePublished = await additionalContentPublicationsRepo.IsSlidePublishedForGroups(course.Id, slide, userGroups.Select(g => g.Id).ToHashSet());

				if (!isSlidePublished)
					return StatusCode((int)HttpStatusCode.Forbidden, $"You have no access to view comments on slide with id {slideId}. You are not in any groups with slide published.");
			}

			var slideIsExercise = slide is ExerciseSlide;

			if (parameters.ForInstructors && !isInstructor)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse($"You can not create comment for instructors. You should be instructor or course admin of course {courseId}."));

			if (parameters.ParentCommentId.HasValue)
			{
				var parentComment = await commentsRepo.FindCommentByIdAsync(parameters.ParentCommentId.Value).ConfigureAwait(false);
				if (parentComment is null || !parentComment.CourseId.EqualsIgnoreCase(courseId) || parentComment.SlideId != slideId || !parentComment.IsTopLevel)
					return BadRequest(new ErrorResponse($"`parentCommentId` comment {parameters.ParentCommentId.Value} not found, belongs to other course, other slide or is not a top-level comment"));

				if (parentComment.IsForInstructorsOnly != parameters.ForInstructors)
					return BadRequest(new ErrorResponse(
						$"`parentCommentId` comment {parameters.ParentCommentId.Value} is {(parentComment.IsForInstructorsOnly ? "" : "not")} for instructors, but new one {(parameters.ForInstructors ? "is" : "is not")}"
					));
			}

			var commentsPolicy = await commentPoliciesRepo.GetCommentsPolicyAsync(courseId).ConfigureAwait(false);

			if (!await CanCommentHereAsync(UserId, courseId, parameters.ParentCommentId.HasValue, commentsPolicy).ConfigureAwait(false))
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You can not create comment here by comments policy."));

			if (!await CanCommentNowAsync(UserId, courseId, commentsPolicy).ConfigureAwait(false))
				return StatusCode((int)HttpStatusCode.TooManyRequests, new ErrorResponse("You are commenting too fast. Please wait some time"));

			if (parameters.Text.Length > CommentsPolicy.MaxCommentLength)
				return StatusCode((int)HttpStatusCode.RequestEntityTooLarge, new ErrorResponse($"Your comment is too large. Max allowed length is {CommentsPolicy.MaxCommentLength} chars"));

			var parentCommentId = parameters.ParentCommentId ?? -1;
			var comment = await commentsRepo.AddCommentAsync(UserId, courseId, slideId, parentCommentId, parameters.ForInstructors, parameters.Text).ConfigureAwait(false);

			if (comment.IsApproved)
				await NotifyAboutNewCommentAsync(comment).ConfigureAwait(false);

			var userAvailableGroupsIds = !isInstructor ? null : (await groupAccessesRepo.GetAvailableForUserGroupsAsync(User.GetUserId(), false, true, true).ConfigureAwait(false)).Select(g => g.Id).ToHashSet();
			var authors2Groups = !isInstructor ? null : await groupMembersRepo.GetUsersGroupsAsync(courseId, new List<string> { UserId }, true).ConfigureAwait(false);
			var passed = slideIsExercise && await visitsRepo.IsPassed(comment.CourseId, comment.SlideId, comment.AuthorId);

			return BuildCommentResponse(
				comment,
				false, new DefaultDictionary<int, List<Comment>>(), new DefaultDictionary<int, int>(), new HashSet<int>(), // canUserSeeNotApprovedComments not used if addReplies == false
				authors2Groups, passed ? new HashSet<string> { comment.AuthorId } : null, userAvailableGroupsIds, false, true, true, false
			);
		}

		private async Task<bool> CanCommentNowAsync(string userId, string courseId, CommentsPolicy commentsPolicy)
		{
			/* Instructors have unlimited comments */
			if (await courseRolesRepo.HasUserAccessToCourse(userId, courseId, CourseRoleType.Instructor).ConfigureAwait(false))
				return true;

			var isUserAddedMaxCommentsInLastTime = await commentsRepo.IsUserAddedMaxCommentsInLastTimeAsync(
				userId,
				commentsPolicy.MaxCommentsCountInLastTime,
				commentsPolicy.LastTimeForMaxCommentsLimit
			).ConfigureAwait(false);
			return !isUserAddedMaxCommentsInLastTime;
		}

		private async Task<bool> CanCommentHereAsync(string userId, string courseId, bool isReply, CommentsPolicy commentsPolicy)
		{
			var isInstructor = await courseRolesRepo.HasUserAccessToCourse(userId, courseId, CourseRoleType.Instructor).ConfigureAwait(false);

			if (!isInstructor && !commentsPolicy.IsCommentsEnabled)
				return false;

			if (isReply && !isInstructor && commentsPolicy.OnlyInstructorsCanReply)
				return false;

			return true;
		}
	}
}