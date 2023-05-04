using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Repos.Groups;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Models.Parameters.Groups;
using Ulearn.Web.Api.Models.Responses.Groups;

namespace Ulearn.Web.Api.Controllers.Groups;

[Route("/super-groups/")]
[Authorize(Policy = "Instructors")]
public class SuperGroupsController : BaseGroupController
{
	private readonly IGroupAccessesRepo groupAccessesRepo;
	private readonly IGroupMembersRepo groupMembersRepo;
	private readonly IGroupsRepo groupsRepo;

	public SuperGroupsController(ICourseStorage courseStorage, UlearnDb db,
		IUsersRepo usersRepo,
		IGroupsRepo groupsRepo, IGroupAccessesRepo groupAccessesRepo, IGroupMembersRepo groupMembersRepo)
		: base(courseStorage, db, usersRepo)
	{
		this.groupsRepo = groupsRepo;
		this.groupAccessesRepo = groupAccessesRepo;
		this.groupMembersRepo = groupMembersRepo;
	}

	/// <summary>
	///     Список супер-групп в курсе
	/// </summary>
	[HttpGet]
	public async Task<ActionResult<SuperGroupsListResponse>> GroupsList([FromQuery] GroupsListParameters parameters)
	{
		return await GetGroupsListResponseAsync(parameters);
	}

	private async Task<SuperGroupsListResponse> GetGroupsListResponseAsync(GroupsListParameters parameters)
	{
		var groups = await groupAccessesRepo.GetAvailableForUserGroupsAsync(parameters.CourseId, UserId, false, true, true, GroupQueryType.SuperGroup);

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
		var groupsIds = filteredGroups
			.Select(g => g.Id)
			.ToList();

		var subGroups = await groupsRepo.FindGroupsBySuperGroupIdsAsync(groupsIds, true);
		var subGroupsIds = subGroups.Select(g => g.Id).ToList();

		var groupMembers = await groupMembersRepo.GetGroupsMembersAsync(subGroupsIds);
		var membersCountByGroup = groupMembers
			.GroupBy(m => m.GroupId)
			.ToDictionary(g => g.Key, g => g.Count())
			.ToDefaultDictionary();

		var superGroupAccessesByGroup = await groupAccessesRepo.GetGroupAccessesAsync(groupsIds);
		var groupAccessesByGroup = await groupAccessesRepo.GetGroupAccessesAsync(subGroupsIds);

		var groupInfos = filteredGroups
			.Select(g => BuildGroupInfo(
				g,
				null,
				superGroupAccessesByGroup[g.Id],
				true))
			.ToList();
		var subGroupInfos = subGroups
			.Select(g => BuildGroupInfo(
				g,
				membersCountByGroup[g.Id],
				groupAccessesByGroup[g.Id],
				true))
			.GroupBy(g => g.SuperGroupId)
			.ToDictionary(g => g.Key!.Value, g => g.ToList());

		return new SuperGroupsListResponse
		{
			SuperGroups = groupInfos,
			SubGroupsBySuperGroupId = subGroupInfos,
			Pagination = new PaginationResponse
			{
				Offset = parameters.Offset,
				Count = filteredGroups.Count,
				TotalCount = groups.Count
			}
		};
	}
}