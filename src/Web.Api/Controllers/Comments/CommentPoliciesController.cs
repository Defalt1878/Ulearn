using System.Threading.Tasks;
using Database;
using Database.Repos;
using Database.Repos.Comments;
using Database.Repos.Groups;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Models.Parameters.Comments;
using Ulearn.Web.Api.Models.Responses.Comments;

namespace Ulearn.Web.Api.Controllers.Comments
{
	[Route("/comment-policies")]
	public class CommentPoliciesController : BaseCommentController
	{
		private readonly ICommentPoliciesRepo commentPoliciesRepo;

		public CommentPoliciesController(ICourseStorage courseStorage, UlearnDb db, IUsersRepo usersRepo,
			ICommentsRepo commentsRepo, ICommentLikesRepo commentLikesRepo, ICoursesRepo coursesRepo, ICourseRolesRepo courseRolesRepo, INotificationsRepo notificationsRepo,
			IAdditionalContentPublicationsRepo additionalContentPublicationsRepo,
			ICommentPoliciesRepo commentPoliciesRepo, IGroupMembersRepo groupMembersRepo, IGroupAccessesRepo groupAccessesRepo, IVisitsRepo visitsRepo, IUnitsRepo unitsRepo)
			: base(courseStorage, db, usersRepo, commentsRepo, commentLikesRepo, coursesRepo, courseRolesRepo, additionalContentPublicationsRepo, notificationsRepo, groupMembersRepo, groupAccessesRepo, visitsRepo, unitsRepo)
		{
			this.commentPoliciesRepo = commentPoliciesRepo;
		}

		/// <summary>
		///     Политика комментариев в курсе
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<CommentPolicyResponse>> Policy([FromQuery] string courseId)
		{
			if (courseId is null || !courseStorage.HasCourse(courseId))
				return NotFound(new ErrorResponse($"Course '{courseId}' not found"));

			var policy = await commentPoliciesRepo.GetCommentsPolicyAsync(courseId).ConfigureAwait(false);
			return new CommentPolicyResponse
			{
				AreCommentsEnabled = policy.IsCommentsEnabled,
				ModerationPolicy = policy.ModerationPolicy,
				OnlyInstructorsCanReply = policy.OnlyInstructorsCanReply
			};
		}

		/// <summary>
		///     Изменить политику комментариев в курсе
		/// </summary>
		[HttpPatch]
		[Authorize(Policy = "CourseAdmins")]
		public async Task<IActionResult> UpdatePolicy([FromQuery] string courseId, [FromBody] UpdatePolicyParameters parameters)
		{
			if (courseId is null || !courseStorage.HasCourse(courseId))
				return NotFound(new ErrorResponse($"Course '{courseId}' not found"));

			var policy = await commentPoliciesRepo.GetCommentsPolicyAsync(courseId).ConfigureAwait(false);

			if (parameters.AreCommentsEnabled.HasValue)
				policy.IsCommentsEnabled = parameters.AreCommentsEnabled.Value;
			if (parameters.ModerationPolicy.HasValue)
				policy.ModerationPolicy = parameters.ModerationPolicy.Value;
			if (parameters.OnlyInstructorsCanReply.HasValue)
				policy.OnlyInstructorsCanReply = parameters.OnlyInstructorsCanReply.Value;

			await commentPoliciesRepo.SaveCommentsPolicyAsync(policy).ConfigureAwait(false);

			return Ok(new SuccessResponseWithMessage($"Comment policy for {courseId} successfully updated"));
		}
	}
}