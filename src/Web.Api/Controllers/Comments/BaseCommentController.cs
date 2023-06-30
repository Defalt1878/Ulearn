using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Models.Comments;
using Database.Repos;
using Database.Repos.Comments;
using Database.Repos.Groups;
using Database.Repos.Users;
using JetBrains.Annotations;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Models.Responses.Comments;
using Ulearn.Web.Api.Utils;

namespace Ulearn.Web.Api.Controllers.Comments
{
	public class BaseCommentController : BaseController
	{
		protected readonly ICommentsRepo commentsRepo;
		protected readonly ICommentLikesRepo commentLikesRepo;
		protected readonly ICoursesRepo coursesRepo;
		protected readonly ICourseRolesRepo courseRolesRepo;
		protected readonly INotificationsRepo notificationsRepo;
		protected readonly IGroupMembersRepo groupMembersRepo;
		protected readonly IGroupAccessesRepo groupAccessesRepo;
		protected readonly IVisitsRepo visitsRepo;
		protected readonly IUnitsRepo unitsRepo;
		protected readonly IAdditionalContentPublicationsRepo additionalContentPublicationsRepo;

		public BaseCommentController(ICourseStorage courseStorage, UlearnDb db, IUsersRepo usersRepo,
			ICommentsRepo commentsRepo, ICommentLikesRepo commentLikesRepo, ICoursesRepo coursesRepo, ICourseRolesRepo courseRolesRepo,
			IAdditionalContentPublicationsRepo additionalContentPublicationsRepo,
			INotificationsRepo notificationsRepo, IGroupMembersRepo groupMembersRepo, IGroupAccessesRepo groupAccessesRepo, IVisitsRepo visitsRepo, IUnitsRepo unitsRepo)
			: base(courseStorage, db, usersRepo)
		{
			this.commentsRepo = commentsRepo;
			this.commentLikesRepo = commentLikesRepo;
			this.coursesRepo = coursesRepo;
			this.courseRolesRepo = courseRolesRepo;
			this.notificationsRepo = notificationsRepo;
			this.groupMembersRepo = groupMembersRepo;
			this.groupAccessesRepo = groupAccessesRepo;
			this.additionalContentPublicationsRepo = additionalContentPublicationsRepo;
			this.visitsRepo = visitsRepo;
			this.unitsRepo = unitsRepo;
		}

		protected List<CommentResponse> BuildCommentsListResponse(IEnumerable<Comment> comments,
			bool canUserSeeNotApprovedComments, DefaultDictionary<int, List<Comment>> replies, DefaultDictionary<int, int> commentLikesCount, HashSet<int> likedByUserCommentsIds,
			Dictionary<string, List<SingleGroup>>? authorId2Groups, HashSet<string>? authorsWithPassed,  HashSet<int>? userAvailableGroups, bool canViewAllGroupMembers, bool addCourseIdAndSlideId, bool addParentCommentId, bool addReplies)
		{
			return comments.Select(c => BuildCommentResponse(c, canUserSeeNotApprovedComments, replies, commentLikesCount, likedByUserCommentsIds,
				authorId2Groups, authorsWithPassed, userAvailableGroups, canViewAllGroupMembers, addCourseIdAndSlideId, addParentCommentId, addReplies)).ToList();
		}

		protected CommentResponse BuildCommentResponse(
			Comment comment,
			bool canUserSeeNotApprovedComments, DefaultDictionary<int, List<Comment>> replies, DefaultDictionary<int, int> commentLikesCount, HashSet<int> likedByUserCommentsIds,
			Dictionary<string, List<SingleGroup>>? authorId2Groups, HashSet<string>? authorsWithPassed, HashSet<int>? userAvailableGroups,
			bool canViewAllGroupMembers, bool addCourseIdAndSlideId, bool addParentCommentId, bool addReplies
		)
		{
			var commentInfo = new CommentResponse
			{
				Id = comment.Id,
				Text = comment.Text,
				RenderedText = CommentTextHelper.RenderCommentTextToHtml(comment.Text),
				Author = BuildShortUserInfo(comment.Author),
				PublishTime = comment.PublishTime,
				IsApproved = comment.IsApproved,
				IsLiked = likedByUserCommentsIds.Contains(comment.Id),
				LikesCount = commentLikesCount[comment.Id],
				IsPassed = authorsWithPassed?.Contains(comment.AuthorId) ?? false,
				Replies = new List<CommentResponse>()
			};

			if (authorId2Groups != null && userAvailableGroups != null && authorId2Groups.ContainsKey(comment.Author.Id))
			{
				commentInfo.AuthorGroups = authorId2Groups[comment.AuthorId]
					.Where(g => canViewAllGroupMembers || userAvailableGroups.Contains(g.Id))
					.Select(BuildShortGroupInfo)
					.ToList().NullIfEmpty();
			}

			if (addCourseIdAndSlideId)
			{
				commentInfo.CourseId = comment.CourseId;
				commentInfo.SlideId = comment.SlideId;
			}

			if (addParentCommentId && !comment.IsTopLevel)
				commentInfo.ParentCommentId = comment.ParentCommentId;

			if (!comment.IsTopLevel)
			{
				commentInfo.IsCorrectAnswer = comment.IsCorrectAnswer;
				return commentInfo;
			}

			commentInfo.IsPinnedToTop = comment.IsPinnedToTop;
			if (addReplies)
			{
				var commentReplies = FilterVisibleComments(replies[comment.Id], canUserSeeNotApprovedComments);
				commentInfo.Replies = BuildCommentsListResponse(commentReplies, canUserSeeNotApprovedComments, null, commentLikesCount, likedByUserCommentsIds,
					authorId2Groups, authorsWithPassed, userAvailableGroups, canViewAllGroupMembers, addCourseIdAndSlideId, addParentCommentId, addReplies);
			}

			return commentInfo;
		}

		protected async Task<bool> CanUserSeeNotApprovedCommentsAsync(string userId, string courseId)
		{
			if (string.IsNullOrEmpty(userId))
				return false;

			var hasCourseAccessForCommentEditing = await coursesRepo.HasCourseAccess(userId, courseId, CourseAccessType.EditPinAndRemoveComments).ConfigureAwait(false);
			var isCourseAdmin = await courseRolesRepo.HasUserAccessToCourse(userId, courseId, CourseRoleType.CourseAdmin).ConfigureAwait(false);
			return isCourseAdmin || hasCourseAccessForCommentEditing;
		}

		protected List<Comment> FilterVisibleComments(List<Comment> comments, bool canUserSeeNotApprovedComments)
		{
			return canUserSeeNotApprovedComments ? comments : comments.Where(c => c.IsApproved || c.AuthorId == UserId).ToList();
		}

		protected async Task NotifyAboutNewCommentAsync(Comment comment)
		{
			var courseId = comment.CourseId;

			if (!comment.IsTopLevel)
			{
				var parentComment = await commentsRepo.FindCommentByIdAsync(comment.ParentCommentId).ConfigureAwait(false);
				if (parentComment != null)
				{
					var replyNotification = new RepliedToYourCommentNotification
					{
						Comment = comment,
						ParentComment = parentComment,
					};
					await notificationsRepo.AddNotification(courseId, replyNotification, comment.AuthorId).ConfigureAwait(false);
				}
			}

			/* Create NewCommentFromStudentFormYourGroupNotification later than RepliedToYourCommentNotification, because the last one is blocker for the first one.
			 * We don't send NewCommentNotification if there is a RepliedToYouCommentNotification */
			var commentFromYourGroupStudentNotification = new NewCommentFromYourGroupStudentNotification { Comment = comment };
			await notificationsRepo.AddNotification(courseId, commentFromYourGroupStudentNotification, comment.AuthorId);

			/* Create NewComment[ForInstructors]Notification later than RepliedToYourCommentNotification and NewCommentFromYourGroupStudentNotification, because the last one is blocker for the first one.
			 * We don't send NewCommentNotification if there is a RepliedToYouCommentNotification or NewCommentFromYourGroupStudentNotification */
			var notification = comment.IsForInstructorsOnly
				? (Notification)new NewCommentForInstructorsOnlyNotification { Comment = comment }
				: new NewCommentNotification { Comment = comment };
			await notificationsRepo.AddNotification(courseId, notification, comment.AuthorId).ConfigureAwait(false);
		}
	}
}