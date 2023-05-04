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
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Swashbuckle.AspNetCore.Annotations;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Models.Parameters.Groups;
using Ulearn.Web.Api.Models.Responses.Groups;
using Vostok.Logging.Abstractions;
using GroupSettings = Ulearn.Web.Api.Models.Responses.Groups.GroupSettings;

namespace Ulearn.Web.Api.Controllers.Groups
{
	[Route("/groups/{groupId:int:min(0)}/")]
	[ProducesResponseType((int)HttpStatusCode.NotFound)]
	[ProducesResponseType((int)HttpStatusCode.Forbidden)]
	[Authorize]
	public class GroupController : BaseGroupController
	{
		private readonly ICourseRolesRepo courseRolesRepo;
		private readonly IGroupAccessesRepo groupAccessesRepo;
		private readonly IGroupMembersRepo groupMembersRepo;
		private readonly IGroupsCreatorAndCopier groupsCreatorAndCopier;
		private readonly IGroupsRepo groupsRepo;
		private readonly INotificationsRepo notificationsRepo;
		private readonly ISlideCheckingsRepo slideCheckingsRepo;
		private readonly IUnitsRepo unitsRepo;

		public GroupController(ICourseStorage courseStorage, UlearnDb db,
			IGroupsRepo groupsRepo, IGroupAccessesRepo groupAccessesRepo, IGroupMembersRepo groupMembersRepo, IUsersRepo usersRepo, ICourseRolesRepo courseRolesRepo, INotificationsRepo notificationsRepo,
			IGroupsCreatorAndCopier groupsCreatorAndCopier, IUnitsRepo unitsRepo, ISlideCheckingsRepo slideCheckingsRepo)
			: base(courseStorage, db, usersRepo)
		{
			this.groupsRepo = groupsRepo;
			this.groupAccessesRepo = groupAccessesRepo;
			this.groupMembersRepo = groupMembersRepo;
			this.courseRolesRepo = courseRolesRepo;
			this.notificationsRepo = notificationsRepo;
			this.groupsCreatorAndCopier = groupsCreatorAndCopier;
			this.unitsRepo = unitsRepo;
			this.slideCheckingsRepo = slideCheckingsRepo;
		}

		private new static ILog Log => LogProvider.Get().ForContext(typeof(GroupController));

		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			var groupId = (int)context.ActionArguments["groupId"];

			var group = await groupsRepo.FindGroupByIdAsync(groupId).ConfigureAwait(false);
			if (group is null)
			{
				context.Result = NotFound(new ErrorResponse($"Group with id {groupId} not found"));
				return;
			}

			var isVisible = await groupAccessesRepo.IsGroupVisibleForUserAsync(group, UserId).ConfigureAwait(false);
			if (!isVisible)
			{
				context.Result = StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You have no access to this group"));
				return;
			}

			context.ActionArguments["group"] = group;

			await base.OnActionExecutionAsync(context, next).ConfigureAwait(false);
		}

		/// <summary>
		///     Информация о группе
		/// </summary>
		[HttpGet]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		public async Task<ActionResult<GroupSettings>> Group([FromRoute] int groupId)
		{
			var group = await groupsRepo.FindGroupByIdAsync(groupId).ConfigureAwait(false);
			var members = await groupMembersRepo.GetGroupMembersAsync(groupId).ConfigureAwait(false);
			var accesses = await groupAccessesRepo.GetGroupAccessesAsync(groupId).ConfigureAwait(false);
			return BuildGroupInfo(group, members.Count, accesses);
		}

		/// <summary>
		///     Изменить группу
		/// </summary>
		[HttpPatch]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		public async Task<ActionResult<GroupSettings>> UpdateGroup([FromRoute] int groupId, [FromBody] UpdateGroupParameters parameters)
		{
			var hasEditAccess = await groupAccessesRepo.HasUserEditAccessToGroupAsync(groupId, UserId).ConfigureAwait(false);
			if (!hasEditAccess)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You have no edit access to this group"));

			var group = await groupsRepo.FindGroupByIdAsync(groupId).ConfigureAwait(false);

			var newName = parameters.Name ?? group.Name;
			var settings = new Database.Models.GroupSettings
			{
				NewName = newName,
				NewIsManualCheckingEnabled = parameters.IsManualCheckingEnabled,
				NewIsManualCheckingEnabledForOldSolutions = parameters.IsManualCheckingEnabledForOldSolutions,
				NewDefaultProhibitFurtherReview = parameters.DefaultProhibitFurtherReview,
				NewCanUsersSeeGroupProgress = parameters.CanStudentsSeeGroupProgress
			};

			switch (group)
			{
				case SingleGroup:
					await groupsRepo.ModifyGroupAsync(groupId, settings).ConfigureAwait(false);
					break;
				case SuperGroup:
					await groupsRepo.ModifyGroupAsync<SuperGroup>(groupId, settings).ConfigureAwait(false);
					break;
			}

			if (parameters.IsArchived.HasValue)
			{
				await groupsRepo.ArchiveGroupAsync(groupId, parameters.IsArchived.Value).ConfigureAwait(false);
				if (parameters.IsArchived.Value)
				{
					var notification = new GroupIsArchivedNotification { GroupId = groupId };
					await notificationsRepo.AddNotification(group.CourseId, notification, UserId);
				}
			}

			if (parameters.IsInviteLinkEnabled.HasValue)
				await groupsRepo.EnableInviteLinkAsync(groupId, parameters.IsInviteLinkEnabled.Value).ConfigureAwait(false);

			return BuildGroupInfo(await groupsRepo.FindGroupByIdAsync(groupId).ConfigureAwait(false));
		}

		/// <summary>
		///     Удалить группу
		/// </summary>
		[HttpDelete]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		public async Task<IActionResult> DeleteGroup([FromRoute] int groupId)
		{
			var hasEditAccess = await groupAccessesRepo.HasUserEditAccessToGroupAsync(groupId, UserId).ConfigureAwait(false);
			if (!hasEditAccess)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You have no edit access to this group"));

			await groupsRepo.DeleteGroupAsync(groupId).ConfigureAwait(false);
			return Ok(new SuccessResponseWithMessage($"Group {groupId} has been deleted"));
		}

		/// <summary>
		///     Сменить владельца группы
		/// </summary>
		[HttpPut("owner")]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		[SwaggerResponse((int)HttpStatusCode.NotFound, Description = "Can't find user or user is not an instructor")]
		public async Task<IActionResult> ChangeOwner([FromRoute] int groupId, [FromBody] ChangeOwnerParameters parameters)
		{
			var group = await groupsRepo.FindGroupByIdAsync(groupId).ConfigureAwait(false);

			var isCourseAdmin = await courseRolesRepo.HasUserAccessToCourse(UserId, group.CourseId, CourseRoleType.CourseAdmin).ConfigureAwait(false);
			var canChangeOwner = group.OwnerId == UserId || isCourseAdmin;
			if (!canChangeOwner)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You can't change the owner of this group. Only current owner and course admin can change the owner."));

			/* New owner should exist and be a course instructor */
			var user = await usersRepo.FindUserById(parameters.OwnerId).ConfigureAwait(false);
			if (user is null)
				return NotFound(new ErrorResponse($"Can't find user with id {parameters.OwnerId}"));
			var isInstructor = await courseRolesRepo.HasUserAccessToCourse(parameters.OwnerId, group.CourseId, CourseRoleType.Instructor).ConfigureAwait(false);
			if (!isInstructor)
				return NotFound(new ErrorResponse($"User {parameters.OwnerId} is not an instructor of course {group.CourseId}"));

			/* Grant full access to previous owner */
			await groupAccessesRepo.GrantAccessAsync(groupId, group.OwnerId, GroupAccessType.FullAccess, UserId).ConfigureAwait(false);
			/* Change owner */
			await groupsRepo.ChangeGroupOwnerAsync(groupId, parameters.OwnerId).ConfigureAwait(false);
			/* Revoke access from new owner */
			await groupAccessesRepo.RevokeAccessAsync(groupId, parameters.OwnerId).ConfigureAwait(false);

			return Ok(new SuccessResponseWithMessage($"New group's owner is {parameters.OwnerId}"));
		}

		/// <summary>
		///     Копирует группу в тот же или другой курс
		/// </summary>
		[HttpPost("copy")]
		[SwaggerResponse((int)HttpStatusCode.Created, Description = "Group has been copied", Type = typeof(CopyGroupResponse))]
		[SwaggerResponse((int)HttpStatusCode.NotFound, Description = "Course not found")]
		[SwaggerResponse((int)HttpStatusCode.NotFound, Description = "Group not found")]
		[SwaggerResponse((int)HttpStatusCode.Forbidden, Description = "You have no access to destination course. You should be instructor or course admin.")]
		public async Task<ActionResult<CopyGroupResponse>> Copy([FromRoute] int groupId, [FromQuery] CopyGroupParameters parameters)
		{
			if (await groupsRepo.FindGroupByIdAsync(groupId).ConfigureAwait(false) is not SingleGroup group)
				return NotFound(new ErrorResponse($"Group {groupId} not found"));
			if (!courseStorage.HasCourse(parameters.DestinationCourseId))
				return NotFound(new ErrorResponse($"Course {parameters.DestinationCourseId} not found"));
			if (!await CanCreateGroupInCourseAsync(UserId, parameters.DestinationCourseId).ConfigureAwait(false))
				return Forbid();

			var newOwnerId = parameters.ChangeOwner ? UserId : null;
			var newGroup = await groupsCreatorAndCopier.CopyGroupAsync(group, parameters.DestinationCourseId, newOwnerId).ConfigureAwait(false);

			var members = await groupMembersRepo.GetGroupMembersAsync(newGroup.Id);
			foreach (var member in members)
				await slideCheckingsRepo.ResetManualCheckingLimitsForUser(parameters.DestinationCourseId, member.UserId);

			var url = Url.Action(new UrlActionContext { Action = nameof(Group), Controller = "Group", Values = new { groupId = group.Id } })!;
			return Created(url, new CopyGroupResponse
			{
				Id = newGroup.Id,
				ApiUrl = url
			});
		}

		private async Task<bool> CanCreateGroupInCourseAsync(string userId, string courseId)
		{
			return await courseRolesRepo.HasUserAccessToCourse(userId, courseId, CourseRoleType.Instructor).ConfigureAwait(false) ||
					await IsSystemAdministratorAsync().ConfigureAwait(false);
		}

		/// <summary>
		///     Список scoring-group курса (примеры: Упражнения, Активность на практике) с информаций о том, включены ли они для
		///     этой группы
		/// </summary>
		[HttpGet("scores")]
		public async Task<ActionResult<GroupScoringGroupsResponse>> ScoringGroups([FromRoute] int groupId)
		{
			var group = await groupsRepo.FindGroupByIdAsync(groupId).ConfigureAwait(false);
			var course = courseStorage.FindCourse(group.CourseId);
			if (course is null)
			{
				Log.Error($"It's strange: group {groupId} exists, but course {group.CourseId} not. I will return 404");
				return NotFound(new ErrorResponse("Group or course not found"));
			}

			var scoringGroups = course.Settings.Scoring.Groups.Values.ToList();
			var visibleUnitIds = await unitsRepo.GetVisibleUnitIds(course, UserId);
			var scoringGroupsCanBeSetInSomeUnit = GetScoringGroupsCanBeSetInSomeUnit(course.GetUnits(visibleUnitIds));
			var enabledScoringGroups = await groupsRepo.GetEnabledAdditionalScoringGroupsForGroupAsync(groupId).ConfigureAwait(false);
			return new GroupScoringGroupsResponse
			{
				Scores = scoringGroups.Select(scoringGroup => BuildGroupScoringGroupInfo(scoringGroup, scoringGroupsCanBeSetInSomeUnit, enabledScoringGroups)).ToList()
			};
		}

		/// <summary>
		///     Сохраняет информацию о том, какие scoring-group включены для группы
		/// </summary>
		[HttpPost("scores")]
		public async Task<IActionResult> SetScoringGroups([FromRoute] int groupId, [FromBody] SetScoringGroupsParameters parameters)
		{
			var hasEditAccess = await groupAccessesRepo.HasUserEditAccessToGroupAsync(groupId, UserId).ConfigureAwait(false);
			if (!hasEditAccess)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You have no edit access to this group"));

			var group = await groupsRepo.FindGroupByIdAsync(groupId).ConfigureAwait(false);
			var course = courseStorage.FindCourse(group.CourseId);
			if (course is null)
			{
				Log.Error($"It's strange: group {groupId} exists, but course {group.CourseId} not. I will return 404");
				return NotFound(new ErrorResponse("Group or course not found"));
			}

			var courseScoringGroupIds = course.Settings.Scoring.Groups.Values.Select(g => g.Id).ToList();
			var visibleUnitIds = await unitsRepo.GetVisibleUnitIds(course, UserId);
			var scoringGroupsCanBeSetInSomeUnit = GetScoringGroupsCanBeSetInSomeUnit(course.GetUnits(visibleUnitIds)).Select(g => g.Id).ToList();
			foreach (var scoringGroupId in parameters.Scores)
			{
				if (!courseScoringGroupIds.Contains(scoringGroupId))
					return NotFound(new ErrorResponse($"Score {scoringGroupId} not found in course {course.Id}"));

				var scoringGroup = course.Settings.Scoring.Groups[scoringGroupId];
				if (scoringGroup.EnabledForEveryone)
					return BadRequest(new ErrorResponse($"You can not enable or disable additional scoring for {scoringGroupId}, because it is enabled for all groups by course creator."));

				if (!scoringGroupsCanBeSetInSomeUnit.Contains(scoringGroupId))
					return BadRequest(new ErrorResponse($"You can not enable or disable additional scoring for {scoringGroupId}, because there is no additional scores for this score in any unit. Contact with course creator."));
			}

			await groupsRepo.EnableAdditionalScoringGroupsForGroupAsync(groupId, parameters.Scores).ConfigureAwait(false);

			return Ok(new SuccessResponseWithMessage($"Scores for group {groupId} updated"));
		}

		/// <summary>
		///     Список студентов группы
		/// </summary>
		[HttpGet("students")]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		public async Task<ActionResult<GroupStudentsResponse>> GroupStudents(int groupId)
		{
			var members = await groupMembersRepo.GetGroupMembersAsync(groupId).ConfigureAwait(false);
			return new GroupStudentsResponse
			{
				Students = members.Select(m => new GroupStudentInfo
				{
					User = BuildShortUserInfo(m.User, true),
					AddingTime = m.AddingTime
				}).ToList()
			};
		}

		/// <summary>
		///     Добавить студента в группу
		/// </summary>
		[HttpPost("students/{studentId:guid}")]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		[SwaggerResponse((int)HttpStatusCode.NotFound, Description = "Can't find user")]
		[SwaggerResponse((int)HttpStatusCode.Conflict, Description = "User is already a student of this group")]
		// ReSharper disable once RouteTemplates.ParameterTypeCanBeMadeStricter
		public async Task<IActionResult> AddStudent(int groupId, string studentId)
		{
			if (!await IsSystemAdministratorAsync().ConfigureAwait(false))
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("Only system administrator can add students to group directly"));

			var user = await usersRepo.FindUserById(studentId).ConfigureAwait(false);
			if (user is null)
				return NotFound(new ErrorResponse($"Can't find user with id {studentId}"));

			var groupMember = await groupMembersRepo.AddUserToGroupAsync(groupId, studentId).ConfigureAwait(false);
			if (groupMember is null)
				return StatusCode((int)HttpStatusCode.Conflict, new ErrorResponse($"User {studentId} is already a student of group {groupId}"));

			await slideCheckingsRepo.ResetManualCheckingLimitsForUser(groupMember.Group.CourseId, studentId);

			return Ok(new SuccessResponseWithMessage($"Student {studentId} is added to group {groupId}"));
		}

		/// <summary>
		///     Удалить студента из группы
		/// </summary>
		[HttpDelete("students/{studentId:guid}")]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		[SwaggerResponse((int)HttpStatusCode.NotFound, Description = "Can't find user or user is not a student of this group")]
		// ReSharper disable once RouteTemplates.ParameterTypeCanBeMadeStricter
		public async Task<IActionResult> RemoveStudent(int groupId, string studentId)
		{
			var hasEditAccess = await groupAccessesRepo.HasUserEditAccessToGroupAsync(groupId, UserId).ConfigureAwait(false);
			if (!hasEditAccess)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You have no edit access to this group"));

			var group = await groupsRepo.FindGroupByIdAsync(groupId).ConfigureAwait(false);

			var user = await usersRepo.FindUserById(studentId).ConfigureAwait(false);
			if (user is null)
				return NotFound(new ErrorResponse($"Can't find user with id {studentId}"));

			var groupMember = await groupMembersRepo.RemoveUserFromGroupAsync(groupId, studentId).ConfigureAwait(false);
			if (groupMember is null)
				return NotFound(new ErrorResponse($"User {studentId} is not a student of group {groupId}"));

			await notificationsRepo.AddNotification(
				group.CourseId,
				new GroupMembersHaveBeenRemovedNotification(groupId, new List<string> { studentId }, usersRepo),
				UserId
			).ConfigureAwait(false);

			return Ok(new SuccessResponseWithMessage($"Student {studentId} is removed from group {groupId}"));
		}

		/// <summary>
		///     Удалить студентов из группы
		/// </summary>
		[HttpDelete("students")]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		public async Task<IActionResult> RemoveStudents(int groupId, RemoveStudentsParameters parameters)
		{
			var hasEditAccess = await groupAccessesRepo.HasUserEditAccessToGroupAsync(groupId, UserId).ConfigureAwait(false);
			if (!hasEditAccess)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You have no edit access to this group"));

			var group = await groupsRepo.FindGroupByIdAsync(groupId).ConfigureAwait(false);

			var members = await groupMembersRepo.RemoveUsersFromGroupAsync(groupId, parameters.StudentIds).ConfigureAwait(false);

			await notificationsRepo.AddNotification(
				group.CourseId,
				new GroupMembersHaveBeenRemovedNotification(group.Id, parameters.StudentIds, usersRepo),
				UserId
			).ConfigureAwait(false);

			return Ok(new SuccessResponseWithMessage($"{members.Count} students have been removed from group {group.Id}"));
		}

		/// <summary>
		///     Скопировать студентов в группу
		/// </summary>
		[HttpPost("students")]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		public async Task<IActionResult> CopyStudents(int groupId, CopyStudentsParameters parameters)
		{
			var destinationGroup = await groupsRepo.FindGroupByIdAsync(groupId).ConfigureAwait(false);
			if (destinationGroup is null)
				return NotFound(new ErrorResponse($"Group {groupId} not found"));

			var hasDestinationGroupEditAccess = await groupAccessesRepo.HasUserEditAccessToGroupAsync(destinationGroup.Id, UserId).ConfigureAwait(false);
			if (!hasDestinationGroupEditAccess)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse($"You have no edit access to group {groupId}"));

			var membersOfAllGroupsAvailableForUser = (await groupAccessesRepo.GetMembersOfAllGroupsVisibleForUserAsync(UserId).ConfigureAwait(false))
				.Select(gm => gm.UserId);

			var studentsToCopySet = parameters.StudentIds.ToHashSet();
			studentsToCopySet.IntersectWith(membersOfAllGroupsAvailableForUser);

			var newMembers = await groupMembersRepo.AddUsersToGroupAsync(groupId, studentsToCopySet).ConfigureAwait(false);
			foreach (var newMember in newMembers)
				await slideCheckingsRepo.ResetManualCheckingLimitsForUser(destinationGroup.CourseId, newMember.UserId);

			await notificationsRepo.AddNotification(
				destinationGroup.CourseId,
				new GroupMembersHaveBeenAddedNotification(groupId, parameters.StudentIds, usersRepo),
				UserId
			).ConfigureAwait(false);

			return Ok(new SuccessResponseWithMessage($"{newMembers.Count} new students have been copied to group {groupId}"));
		}

		/// <summary>
		///     Cбросить факт просмотра чужих решений упражнений.
		///     Обнулить количество попыток автоматических тестов, сохранив полученные баллы
		/// </summary>
		[HttpPost("students/reset-limits")]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		public async Task<IActionResult> ResetLimitsForStudents(int groupId, ResetLimitsParameters parameters)
		{
			var hasEditAccess = await groupAccessesRepo.HasUserEditAccessToGroupAsync(groupId, UserId);
			if (!hasEditAccess)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You have no edit access to this group"));

			var group = await groupsRepo.FindGroupByIdAsync(groupId);
			var members = await groupMembersRepo.GetGroupsMembersIdsAsync(new[] { groupId });

			var studentsToProcessSet = parameters.StudentIds.ToHashSet();
			studentsToProcessSet.IntersectWith(members);

			foreach (var studentId in studentsToProcessSet)
				await slideCheckingsRepo.ResetAutomaticCheckingLimitsForUser(group.CourseId, studentId);

			return Ok(new SuccessResponseWithMessage($"Limits for {studentsToProcessSet.Count} students have been reset"));
		}

		/// <summary>
		///     Список доступов к группе
		/// </summary>
		[HttpGet("accesses")]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		public async Task<ActionResult<GroupAccessesResponse>> GroupAccesses(int groupId)
		{
			var accesses = await groupAccessesRepo.GetGroupAccessesAsync(groupId).ConfigureAwait(false);
			return new GroupAccessesResponse
			{
				Accesses = accesses.Select(BuildGroupAccessesInfo).ToList()
			};
		}

		/// <summary>
		///     Выдать доступ к группе
		/// </summary>
		[HttpPost("accesses/{userId:guid}")]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		[SwaggerResponse((int)HttpStatusCode.Conflict, "User already has access to group")]
		// ReSharper disable once RouteTemplates.ParameterTypeCanBeMadeStricter
		public async Task<IActionResult> GrantAccess(int groupId, string userId)
		{
			var hasEditAccess = await groupAccessesRepo.HasUserEditAccessToGroupAsync(groupId, UserId).ConfigureAwait(false);
			if (!hasEditAccess)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You have no edit access to this group"));

			var group = await groupsRepo.FindGroupByIdAsync(groupId).ConfigureAwait(false);

			var user = await usersRepo.FindUserById(userId).ConfigureAwait(false);
			if (user is null)
				return NotFound(new ErrorResponse($"User {userId} not found"));

			var alreadyHasAccess = await groupAccessesRepo.HasUserGrantedAccessToGroupOrIsOwnerAsync(groupId, userId).ConfigureAwait(false);
			if (alreadyHasAccess)
				return Conflict(new ErrorResponse($"User {userId} already has access to group {groupId}"));

			var access = await groupAccessesRepo.GrantAccessAsync(groupId, userId, GroupAccessType.FullAccess, UserId).ConfigureAwait(false);
			await notificationsRepo.AddNotification(group.CourseId, new GrantedAccessToGroupNotification { AccessId = access.Id }, UserId).ConfigureAwait(false);

			return Ok(new SuccessResponseWithMessage($"User {userId} has full access to {groupId}"));
		}

		/// <summary>
		///     Отозвать доступ к группе
		/// </summary>
		[HttpDelete("accesses/{userId:guid}")]
		[ProducesResponseType((int)HttpStatusCode.OK)]
		// ReSharper disable once RouteTemplates.ParameterTypeCanBeMadeStricter
		public async Task<IActionResult> RevokeAccess(int groupId, string userId)
		{
			var group = await groupsRepo.FindGroupByIdAsync(groupId).ConfigureAwait(false);

			var canRevokeAccess = await groupAccessesRepo.CanRevokeAccessAsync(groupId, userId, UserId).ConfigureAwait(false) ||
								await IsSystemAdministratorAsync().ConfigureAwait(false);
			if (!canRevokeAccess)
				return Forbid();

			var hasAccess = await groupAccessesRepo.HasUserGrantedAccessToGroupOrIsOwnerAsync(groupId, userId).ConfigureAwait(false);
			if (!hasAccess)
				return NotFound(new ErrorResponse($"User {userId} has no access to group {groupId}"));

			var accesses = await groupAccessesRepo.RevokeAccessAsync(groupId, userId).ConfigureAwait(false);
			foreach (var access in accesses)
				await notificationsRepo.AddNotification(
					group.CourseId,
					new RevokedAccessToGroupNotification { AccessId = access.Id },
					UserId
				).ConfigureAwait(false);

			return Ok(new SuccessResponseWithMessage($"User {userId} has no access to {groupId}"));
		}
	}
}