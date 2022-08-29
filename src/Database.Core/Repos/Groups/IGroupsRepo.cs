﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database.Models;
using Ulearn.Core.Courses;

namespace Database.Repos.Groups
{
	public interface IGroupsRepo
	{
		Task<Group> CreateGroupAsync(
			string courseId,
			string name,
			string ownerId,
			bool isManualCheckingEnabled = false,
			bool isManualCheckingEnabledForOldSolutions = false,
			bool canUsersSeeGroupProgress = true,
			bool defaultProhibitFurtherReview = true,
			bool isInviteLinkEnabled = true);

		Task<Group> CopyGroupAsync(Group group, string courseId, string newOwnerId = "");

		Task<Group> ModifyGroupAsync(
			int groupId,
			string newName,
			bool newIsManualCheckingEnabled,
			bool newIsManualCheckingEnabledForOldSolutions,
			bool newDefaultProhibitFurtherReview,
			bool newCanUsersSeeGroupProgress);

		Task ChangeGroupOwnerAsync(int groupId, string newOwnerId);
		Task<Group> ArchiveGroupAsync(int groupId, bool isArchived);
		Task DeleteGroupAsync(int groupId);
		Task<Group> FindGroupByIdAsync(int groupId);
		Task<Group> FindGroupByInviteHashAsync(Guid hash);
		Task<List<Group>> GetCourseGroupsAsync(string courseId, bool includeArchived = false);
		Task<List<Group>> GetMyGroupsFilterAccessibleToUserAsync(string courseId, string userId, bool includeArchived = false);
		Task EnableInviteLinkAsync(int groupId, bool isEnabled);
		Task<bool> IsManualCheckingEnabledForUserAsync(Course course, string userId);
		Task<bool> GetDefaultProhibitFurtherReviewForUserAsync(string courseId, string userId, string instructorId);
		Task EnableAdditionalScoringGroupsForGroupAsync(int groupId, IEnumerable<string> scoringGroupsIds);
		Task<List<EnabledAdditionalScoringGroup>> GetEnabledAdditionalScoringGroupsAsync(string courseId);
		Task<List<EnabledAdditionalScoringGroup>> GetEnabledAdditionalScoringGroupsForGroupAsync(int groupId);
		IQueryable<Group> GetCourseGroupsQueryable(string courseId, bool includeArchived = false);
		Task<HashSet<string>> GetUsersIdsWithEnabledManualChecking(Course course, List<string> userIds);
		Task<List<string>> GetUsersIdsForAllGroups(string courseId);
		Task<List<Group>> GetMyGroupsFilterAccessibleToUser(string courseId, string userId, bool includeArchived = false);
		Task<List<string>> GetGroupsMembersAsUserIds(IEnumerable<int> groupIds);
		Task<IEnumerable<(int GroupId, string UserId)>> GetGroupsMembersAsGroupsIdsAndUserIds(IEnumerable<int> groupIds);
		List<ApplicationUser> GetGroupsMembersAsUsers(IEnumerable<int> groupIds);
		Task<bool> GetDefaultProhibitFutherReviewForUser(string courseId, string userId, string instructorId);
	}
}