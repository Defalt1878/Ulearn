using System;
using System.Collections.Immutable;
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
using Microsoft.AspNetCore.Mvc.Routing;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Authorization;
using Ulearn.Web.Api.Models.Parameters.Groups;
using Ulearn.Web.Api.Models.Responses.Groups;

namespace Ulearn.Web.Api.Controllers.Groups
{
	[Route("/groups/")]
	public class GroupsController : BaseGroupController
	{
		private readonly IGroupAccessesRepo groupAccessesRepo;
		private readonly IGroupMembersRepo groupMembersRepo;
		private readonly IGroupsRepo groupsRepo;
		private readonly INotificationsRepo notificationsRepo;

		public GroupsController(ICourseStorage courseStorage, UlearnDb db,
			IUsersRepo usersRepo,
			IGroupsRepo groupsRepo, IGroupAccessesRepo groupAccessesRepo, IGroupMembersRepo groupMembersRepo, INotificationsRepo notificationsRepo)
			: base(courseStorage, db, usersRepo)
		{
			this.groupsRepo = groupsRepo;
			this.groupAccessesRepo = groupAccessesRepo;
			this.groupMembersRepo = groupMembersRepo;
			this.notificationsRepo = notificationsRepo;
		}

		/// <summary>
		///     Список групп в курсе
		/// </summary>
		[HttpGet]
		[Authorize(Policy = "Instructors")]
		public async Task<ActionResult<GroupsListResponse>> GroupsList([FromQuery] GroupsListParameters parameters)
		{
			return await GetGroupsListResponseAsync(parameters);
		}

		private async Task<GroupsListResponse> GetGroupsListResponseAsync(GroupsListParameters parameters)
		{
			var groups = (await groupAccessesRepo.GetAvailableForUserGroupsAsync(parameters.CourseId, UserId, false, true, parameters.Archived, GroupQueryType.SingleGroup))
				.Cast<SingleGroup>()
				.ToList();
			var superGroupsIds = groups
				.Select(g => g.SuperGroupId)
				.Distinct()
				.Where(id => id is not null)
				.Cast<int>()
				.ToList();
			var superGroups = (await groupsRepo.FindGroupsByIdsAsync<SuperGroup>(superGroupsIds))
				.ToDictionary(g => g.Id);
			if (parameters.UserId is not null)
			{
				var groupsWithUserAsMemberIds = (await groupMembersRepo.GetUserGroupsIdsAsync(parameters.CourseId, parameters.UserId, parameters.Archived)).ToHashSet();
				groups = groups.Where(g => groupsWithUserAsMemberIds.Contains(g.Id)).ToList();
			}

			/* Order groups by (name, createTime) and get one page of data (offset...offset+count) */
			var groupIds = groups
				.OrderBy(g => g.Name, StringComparer.InvariantCultureIgnoreCase)
				.ThenBy(g => g.Id)
				.Skip(parameters.Offset)
				.Take(parameters.Count)
				.Select(g => g.Id)
				.ToImmutableHashSet();

			var filteredGroups = groups
				.Where(g => groupIds.Contains(g.Id))
				.ToList();

			var groupMembers = await groupMembersRepo.GetGroupsMembersAsync(groupIds);
			var membersCountByGroup = groupMembers.GroupBy(m => m.GroupId).ToDictionary(g => g.Key, g => g.Count()).ToDefaultDictionary();

			var groupAccessesByGroup = await groupAccessesRepo.GetGroupAccessesAsync(groupIds);

			var groupInfos = filteredGroups.Select(g => BuildGroupInfo(
				g,
				membersCountByGroup[g.Id],
				groupAccessesByGroup[g.Id],
				true,
				superGroupName: g.SuperGroupId.HasValue && superGroups.TryGetValue(g.SuperGroupId.Value, out var group) ? group.Name : null
			)).ToList();

			return new GroupsListResponse
			{
				Groups = groupInfos,
				Pagination = new PaginationResponse
				{
					Offset = parameters.Offset,
					Count = filteredGroups.Count,
					TotalCount = groups.Count
				}
			};
		}

		/// <summary>
		///     Создать новую группу в курсе
		/// </summary>
		/// <param name="parameters">Название новой группы</param>
		[HttpPost]
		[Authorize(Policy = "Instructors")]
		[ProducesResponseType((int)HttpStatusCode.Created)]
		public async Task<ActionResult<CreateGroupResponse>> CreateGroup([FromQuery] CourseAuthorizationParameters courseAuthorizationParameters, CreateGroupParameters parameters)
		{
			var ownerId = User.GetUserId();
			var group = await groupsRepo.CreateGroupAsync(courseAuthorizationParameters.CourseId, parameters.Name, ownerId, parameters.GroupType);

			await notificationsRepo.AddNotification(
				group.CourseId,
				new CreatedGroupNotification(group.Id),
				UserId
			);

			var url = Url.Action(new UrlActionContext { Action = nameof(GroupController.Group), Controller = "Group", Values = new { groupId = group.Id } })!;
			return Created(url, new CreateGroupResponse
			{
				Id = group.Id,
				ApiUrl = url
			});
		}
	}
}