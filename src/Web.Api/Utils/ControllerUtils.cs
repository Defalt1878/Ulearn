using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses;

namespace Ulearn.Web.Api.Utils
{
	public class ControllerUtils
	{
		private readonly ICourseRolesRepo courseRolesRepo;
		private readonly IGroupAccessesRepo groupAccessesRepo;
		private readonly IGroupMembersRepo groupMembersRepo;
		private readonly IGroupsRepo groupsRepo;

		public ControllerUtils(IGroupMembersRepo groupMembersRepo, ICourseRolesRepo courseRolesRepo, IGroupsRepo groupsRepo, IGroupAccessesRepo groupAccessesRepo)
		{
			this.groupMembersRepo = groupMembersRepo;
			this.courseRolesRepo = courseRolesRepo;
			this.groupsRepo = groupsRepo;
			this.groupAccessesRepo = groupAccessesRepo;
		}

		public async Task<T> GetFilterOptionsByGroup<T>(string userId, string courseId,
			List<string> groupsIds, bool allowSeeGroupForAnyMember = false) where T : AbstractFilterOptionByCourseAndUsers, new()
		{
			var result = new T { CourseId = courseId };
			var isCourseAdmin = await courseRolesRepo.HasUserAccessToCourse(userId, courseId, CourseRoleType.Instructor);

			/* if groupsIds contains "all" (it should be exclusive), get all users. Available only for course admins */
			if (groupsIds.Contains("all") && isCourseAdmin)
				return result;
			/* if groupsIds contains "not-group" (it should be exclusive), get all users not in any groups, available only for course admins */
			if (groupsIds.Contains("not-in-group") && isCourseAdmin)
			{
				var usersInGroups = await groupMembersRepo.GetUsersIdsForAllCourseGroupsAsync(courseId);
				result.UserIds = usersInGroups.ToList();
				result.IsUserIdsSupplement = true;
				return result;
			}

			result.UserIds = new List<string>();

			/* if groupsIds is empty, get members of all groups user has access to. Available for instructors */
			if ((groupsIds.Count == 0 || groupsIds.Any(string.IsNullOrEmpty)) && isCourseAdmin)
			{
				var accessibleGroupsIds = (await groupsRepo.GetMyGroupsFilterAccessibleToUserAsync(courseId, userId)).Select(g => g.Id).ToList();
				var groupUsersIdsQuery = await groupMembersRepo.GetGroupsMembersIdsAsync(accessibleGroupsIds);
				result.UserIds = groupUsersIdsQuery.ToList();
				return result;
			}

			var usersIds = new HashSet<string>();
			var groupsIdsInts = groupsIds.Select(s => int.TryParse(s, out var i) ? i : (int?)null).Where(i => i.HasValue).Select(i => i.Value).ToList();
			var group2GroupMembersIds = (await groupMembersRepo.GetGroupsMembersAsGroupsIdsAndUserIds(groupsIdsInts))
				.GroupBy(u => u.GroupId)
				.ToDictionary(g => g.Key, g => g.Select(p => p.UserId).ToList());
			foreach (var groupIdInt in groupsIdsInts)
			{
				if (!group2GroupMembersIds.ContainsKey(groupIdInt))
					continue;
				// var hasAccessToGroup = groupsRepo.IsGroupAvailableForUser(groupIdInt, User);
				var hasAccessToGroup = await groupAccessesRepo.IsGroupVisibleForUserAsync(groupIdInt, userId);
				if (allowSeeGroupForAnyMember)
					hasAccessToGroup |= group2GroupMembersIds[groupIdInt].Contains(userId);
				if (hasAccessToGroup)
					usersIds.UnionWith(group2GroupMembersIds[groupIdInt]);
			}

			result.UserIds = usersIds.ToList();
			return result;
		}

		public async Task<List<string>> GetEnabledAdditionalScoringGroupsForGroups(Course course, List<string> groupsIds, string userId, bool includeArchived = false)
		{
			if (groupsIds.Contains("all") || groupsIds.Contains("not-in-group"))
				return course.Settings.Scoring.Groups.Keys.ToList();

			var enabledAdditionalScoringGroupsForGroups = (await groupsRepo.GetEnabledAdditionalScoringGroupsAsync(course.Id, includeArchived))
				.GroupBy(e => e.GroupId)
				.ToDictionary(g => g.Key, g => g.Select(e => e.ScoringGroupId).ToList());

			/* if groupsIds is empty, get members of all own groups. Available for instructors */
			if (groupsIds.Count == 0 || groupsIds.Any(string.IsNullOrEmpty))
			{
				var accessibleGroupsIds = (await groupsRepo.GetMyGroupsFilterAccessibleToUserAsync(course.Id, userId, includeArchived)).Select(g => g.Id).ToList();
				return enabledAdditionalScoringGroupsForGroups.Where(kv => accessibleGroupsIds.Contains(kv.Key)).SelectMany(kv => kv.Value).ToList();
			}

			var result = new List<string>();
			foreach (var groupId in groupsIds)
				if (int.TryParse(groupId, out var groupIdInt))
					result.AddRange(enabledAdditionalScoringGroupsForGroups.GetOrDefault(groupIdInt, new List<string>()));

			return result;
		}
	}
}