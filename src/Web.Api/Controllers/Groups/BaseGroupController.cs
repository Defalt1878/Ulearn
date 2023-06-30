using System;
using System.Collections.Generic;
using System.Linq;
using Database;
using Database.Models;
using Database.Repos.Users;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Routing;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Units;
using Ulearn.Web.Api.Models.Responses.Groups;
using GroupSettings = Ulearn.Web.Api.Models.Responses.Groups.GroupSettings;

namespace Ulearn.Web.Api.Controllers.Groups
{
	public abstract class BaseGroupController : BaseController
	{
		protected BaseGroupController(ICourseStorage courseStorage, UlearnDb db, IUsersRepo usersRepo)
			: base(courseStorage, db, usersRepo)
		{
		}

		protected GroupSettings BuildGroupInfo(GroupBase group,
			int? membersCount = null,
			IEnumerable<GroupAccess> accesses = null,
			bool addGroupApiUrl = false,
			bool? isLinkEnabled = null,
			bool? isUserMemberOfGroup = null,
			string? superGroupName = null)
		{
			if (group == null)
				throw new ArgumentNullException(nameof(group));

			var isManualCheckingEnabled = (bool?)null;
			var isManualCheckingEnabledForOldSolutions = (bool?)null;
			var defaultProhibitFutherReview = (bool?)null;
			var canUsersSeeGroupProgress = (bool?)null;
			var superGroupId = (int?)null;

			var distributionTableLink = (string)null;

			if (group is SingleGroup singleGroup)
			{
				isManualCheckingEnabled = singleGroup.IsManualCheckingEnabled;
				isManualCheckingEnabledForOldSolutions = singleGroup.IsManualCheckingEnabledForOldSolutions;
				defaultProhibitFutherReview = singleGroup.DefaultProhibitFutherReview;
				canUsersSeeGroupProgress = singleGroup.CanUsersSeeGroupProgress;
				superGroupId = singleGroup.SuperGroupId;
			}

			if (group is SuperGroup superGroup)
			{
				distributionTableLink = superGroup.DistributionTableLink;
			}

			var course = courseStorage.GetCourse(group.CourseId);

			if (course == null)
				throw new ArgumentException(nameof(group.CourseId));

			return new GroupSettings
			{
				Id = group.Id,
				CourseTitle = course.Title,
				CourseId = course.Id,
				GroupType = group.GroupType,
				CreateTime = group.CreateTime,
				Name = group.Name,
				Owner = BuildShortUserInfo(group.Owner),
				InviteHash = group.InviteHash,
				IsInviteLinkEnabled = isLinkEnabled ?? group.IsInviteLinkEnabled,
				IsArchived = group.IsArchived,
				AreYouStudent = isUserMemberOfGroup,

				IsManualCheckingEnabled = isManualCheckingEnabled,
				IsManualCheckingEnabledForOldSolutions = isManualCheckingEnabledForOldSolutions,
				DefaultProhibitFurtherReview = defaultProhibitFutherReview,
				CanStudentsSeeGroupProgress = canUsersSeeGroupProgress,

				StudentsCount = membersCount,
				Accesses = accesses?.Select(BuildGroupAccessesInfo).ToList(),

				ApiUrl = addGroupApiUrl ? Url.Action(new UrlActionContext { Action = nameof(GroupController.Group), Controller = "Group", Values = new { groupId = group.Id } }) : null,
				SuperGroupId = superGroupId,
				SuperGroupName = superGroupName,
				DistributionTableLink = distributionTableLink
			};
		}

		protected GroupAccessesInfo BuildGroupAccessesInfo(GroupAccess access)
		{
			if (access == null)
				throw new ArgumentNullException(nameof(access));

			return new GroupAccessesInfo
			{
				User = BuildShortUserInfo(access.User),
				AccessType = access.AccessType,
				GrantedBy = BuildShortUserInfo(access.GrantedBy),
				GrantTime = access.GrantTime
			};
		}

		protected static List<ScoringGroup> GetScoringGroupsCanBeSetInSomeUnit(IEnumerable<Unit> units)
		{
			return units
				.SelectMany(u => u.Scoring.Groups.Values)
				.Where(g => g.CanBeSetByInstructor && !g.EnabledForEveryone)
				.DistinctBy(g => g.Id)
				.ToList();
		}

		protected static GroupScoringGroupInfo BuildGroupScoringGroupInfo(ScoringGroup scoringGroup, List<ScoringGroup> scoringGroupsCanBeSetInSomeUnit, List<EnabledAdditionalScoringGroup> enabledScoringGroups, int groupsCount = 1)
		{
			var canBeSetByInstructorInSomeUnit = scoringGroupsCanBeSetInSomeUnit.Select(g => g.Id).Contains(scoringGroup.Id);
			var isEnabledManuallyCount = enabledScoringGroups
				.Select(g => g.ScoringGroupId)
				.Where(sg => sg == scoringGroup.Id)
				.ToList()
				.Count;
			return new GroupScoringGroupInfo(scoringGroup)
			{
				AreAdditionalScoresEnabledForAllGroups = scoringGroup.EnabledForEveryone,
				CanInstructorSetAdditionalScoreInSomeUnit = canBeSetByInstructorInSomeUnit,
				AreAdditionalScoresEnabledInThisGroup = (scoringGroup.EnabledForEveryone || !canBeSetByInstructorInSomeUnit)
					? null
					: isEnabledManuallyCount == groupsCount
						? true //enabled for all groups
						: isEnabledManuallyCount == 0
							? false //disabled for all groups
							: null //uncertain
			};
		}
	}
}