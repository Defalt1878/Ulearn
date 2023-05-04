using System;
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
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Utils;
using GroupSettings = Ulearn.Web.Api.Models.Responses.Groups.GroupSettings;

namespace Ulearn.Web.Api.Controllers.Groups
{
	[Route("/groups/{inviteHash:guid}")]
	[ProducesResponseType((int)HttpStatusCode.NotFound)]
	[Authorize]
	public class JoinGroupController : BaseGroupController
	{
		private readonly IGroupMembersRepo groupMembersRepo;
		private readonly IGroupsRepo groupsRepo;
		private readonly ISlideCheckingsRepo slideCheckingsRepo;
		private readonly SuperGroupManager superGroupManager;

		public JoinGroupController(ICourseStorage courseStorage, UlearnDb db,
			IUsersRepo usersRepo,
			IGroupsRepo groupsRepo,
			SuperGroupManager superGroupManager,
			IGroupMembersRepo groupMembersRepo,
			ISlideCheckingsRepo slideCheckingsRepo)
			: base(courseStorage, db, usersRepo)
		{
			this.groupsRepo = groupsRepo;
			this.groupMembersRepo = groupMembersRepo;
			this.slideCheckingsRepo = slideCheckingsRepo;
			this.superGroupManager = superGroupManager;
		}

		/// <summary>
		///     Найти группу по инвайт-хешу.
		///     Группа должна быть не удалена, а инвайт-ссылка в ней — включена.
		/// </summary>
		/// <param name="inviteHash">Инвайт-хеш группы</param>
		[HttpGet]
		public async Task<ActionResult<GroupSettings>> Group(Guid inviteHash)
		{
			var group = await groupsRepo.FindGroupByInviteHashAsync_WithDisabledLink(inviteHash).ConfigureAwait(false);

			if (group is null)
				return NotFound(new ErrorResponse($"Group with invite hash {inviteHash} not found"));

			var isLinkEnabled = group.IsInviteLinkEnabled;

			if (group.GroupType == GroupType.SuperGroup)
			{
				var (createdGroups, error) = await GetGroupsToJoinViaSuperGroup(group as SuperGroup);
				if (error is null)
					group = createdGroups.First();
			}

			var isMember = await groupMembersRepo.IsUserMemberOfGroup(group.Id, UserId).ConfigureAwait(false);
			return BuildGroupInfo(group, isUserMemberOfGroup: isMember, isLinkEnabled: isLinkEnabled);
		}

		/// <summary>
		///     Вступить в группу по инвайт-хешу.
		///     Группа должна быть не удалена, а инвайт-ссылка в ней — включена.
		/// </summary>
		/// <param name="inviteHash">Инвайт-хеш группы</param>
		[HttpPost("join")]
		[SwaggerResponse((int)HttpStatusCode.Conflict, Description = "User is already a student of this group")]
		public async Task<IActionResult> Join(Guid inviteHash)
		{
			var group = await groupsRepo.FindGroupByInviteHashAsync(inviteHash).ConfigureAwait(false);

			if (group is null)
				return NotFound(new ErrorResponse($"Group with invite hash {inviteHash} not found"));

			if (group.GroupType == GroupType.SuperGroup)
				return await JoinSuperGroup(group as SuperGroup);

			var groupMember = await groupMembersRepo.AddUserToGroupAsync(group.Id, UserId).ConfigureAwait(false);
			if (groupMember is null)
				return StatusCode((int)HttpStatusCode.Conflict, new ErrorResponse($"User {UserId} is already a student of group {group.Id}"));

			await slideCheckingsRepo.ResetManualCheckingLimitsForUser(group.CourseId, UserId);

			return Ok(new SuccessResponseWithMessage($"Student {UserId} is added to group {group.Id}"));
		}

		private async Task<ActionResult> JoinSuperGroup(SuperGroup superGroup)
		{
			var (createdGroups, error) = await GetGroupsToJoinViaSuperGroup(superGroup);

			if (error is not null)
				return NotFound(error);

			var groupToJoin = createdGroups.First();

			var groupMember = await groupMembersRepo.AddUserToGroupAsync(groupToJoin.Id, UserId).ConfigureAwait(false);
			return groupMember is null
				? Conflict(new ErrorResponse($"User {UserId} is already a student of group {groupToJoin.Id}"))
				: Ok(new SuccessResponseWithMessage($"Student {UserId} is added to group {superGroup.Id}"));
		}

		private async Task<(List<SingleGroup> createdGroups, string error)> GetGroupsToJoinViaSuperGroup(SuperGroup superGroup)
		{
			var (createdGroups, spreadSheetGroups) = await superGroupManager.GetSheetGroupsAndCreatedGroups(superGroup.DistributionTableLink, superGroup.Id);
			var user = await usersRepo.FindUserById(UserId);

			var userNames = new HashSet<string> { $"{user!.FirstName.Trim()} {user.LastName.Trim()}", $"{user.LastName.Trim()} {user.FirstName.Trim()}" };
			var userGroupsNames = spreadSheetGroups
				.Where(g => userNames.Contains(g.studentName))
				.Select(g => g.groupName)
				.ToHashSet();

			return (createdGroups
					.Where(g => userGroupsNames.Contains(g.Name))
					.ToList(),
				userGroupsNames.Count != 1
					? "There is multiple or none groups user could join in"
					: null);
		}
	}
}