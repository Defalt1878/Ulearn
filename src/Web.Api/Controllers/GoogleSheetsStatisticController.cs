﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ulearn.Common.Extensions;
using Ulearn.Core.Configuration;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.GoogleSheet;
using Ulearn.Web.Api.Models;
using Ulearn.Web.Api.Models.Parameters;
using Ulearn.Web.Api.Models.Parameters.Analytics;
using Ulearn.Web.Api.Models.Responses;
using Ulearn.Web.Api.Models.Responses.Courses;
using Ulearn.Web.Api.Utils;
using Web.Api.Configuration;


namespace Ulearn.Web.Api.Controllers
{
	[Route("/course-score-sheet/export/to-google-sheets")]
	public class GoogleSheetsStatisticController : BaseController
	{
		private readonly ICourseRolesRepo courseRolesRepo;
		private readonly IGroupAccessesRepo groupAccessesRepo;
		private readonly IGoogleSheetExportTasksRepo googleSheetExportTasksRepo;
		private readonly UlearnConfiguration configuration;
		private readonly StatisticModelUtils statisticModelUtils;

		public GoogleSheetsStatisticController(ICourseStorage courseStorage, UlearnDb db,
			IUsersRepo usersRepo, ICourseRolesRepo courseRolesRepo, IGroupAccessesRepo groupAccessesRepo, IOptions<WebApiConfiguration> options,
			IGoogleSheetExportTasksRepo googleSheetExportTasksRepo, StatisticModelUtils statisticModelUtils)
			: base(courseStorage, db, usersRepo)
		{
			this.courseRolesRepo = courseRolesRepo;
			this.groupAccessesRepo = groupAccessesRepo;
			this.googleSheetExportTasksRepo = googleSheetExportTasksRepo;
			this.statisticModelUtils = statisticModelUtils;
			configuration = options.Value;
		}

		[HttpGet("tasks")]
		[Authorize(Policy = "Instructors")]
		public async Task<ActionResult<GoogleSheetsExportTaskListResponse>> GetAllCourseTasks([FromQuery] string courseId)
		{
			if (!await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.Instructor))
				return Forbid();
			var isCourseAdmin = await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.CourseAdmin);

			var exportTasks = await googleSheetExportTasksRepo.GetTasks(courseId, isCourseAdmin ? null : UserId);
			var sortedTasks = exportTasks
				.OrderBy(t => t.AuthorId == UserId)
				.ThenByDescending(t => t.RefreshEndDate)
				.ThenByDescending(t => t.Groups.Count)
				.ToList();
			
			var responses = sortedTasks
				.Select(task => new GoogleSheetsExportTaskResponse
				{
					Id = task.Id,
					AuthorInfo = BuildShortUserInfo(task.Author),
					Groups = task.Groups.Select(e => BuildShortGroupInfo(e.Group)).ToList(),
					IsVisibleForStudents = task.IsVisibleForStudents,
					RefreshStartDate = task.RefreshStartDate,
					RefreshEndDate = task.RefreshEndDate,
					RefreshTimeInMinutes = task.RefreshTimeInMinutes,
					SpreadsheetId = task.SpreadsheetId,
					ListId = task.ListId
				})
				.ToList();
			return new GoogleSheetsExportTaskListResponse
			{
				GoogleSheetsExportTasks = responses
			};
		}

		[HttpPost("tasks")]
		[Authorize]
		public async Task<ActionResult<GoogleSheetsExportTaskResponse>> AddNewTask([FromBody] GoogleSheetsExportTaskParams param)
		{
			if (!await HasAccessToGroups(param.CourseId, param.GroupsIds))
				return Forbid();

			var id = await googleSheetExportTasksRepo.AddTask(param.CourseId, UserId, param.IsVisibleForStudents,
				param.RefreshStartDate, param.RefreshEndDate, param.RefreshTimeInMinutes, param.GroupsIds,
				param.SpreadsheetId, param.ListId);

			var exportTask = await googleSheetExportTasksRepo.GetTaskById(id);

			var result = new GoogleSheetsExportTaskResponse
			{
				Id = exportTask.Id,
				AuthorInfo = BuildShortUserInfo(exportTask.Author),
				Groups = exportTask.Groups.Select(e => BuildShortGroupInfo(e.Group)).ToList(),
				IsVisibleForStudents = exportTask.IsVisibleForStudents,
				RefreshStartDate = exportTask.RefreshStartDate,
				RefreshEndDate = exportTask.RefreshEndDate,
				RefreshTimeInMinutes = exportTask.RefreshTimeInMinutes,
				SpreadsheetId = exportTask.SpreadsheetId,
				ListId = exportTask.ListId
			};
			return result;
		}

		[HttpPost("tasks/{taskId}")]
		[Authorize]
		public async Task<ActionResult> ExportTaskNow([FromRoute] int taskId)
		{
			var task = await googleSheetExportTasksRepo.GetTaskById(taskId);
			if (task == null)
				return NotFound();
			if (!await courseRolesRepo.HasUserAccessToCourse(UserId, task.CourseId, CourseRoleType.CourseAdmin) && task.AuthorId != UserId)
				return Forbid();

			var courseStatisticsParams = new CourseStatisticsParams
			{
				CourseId = task.CourseId,
				ListId = task.ListId,
				GroupsIds = task.Groups.Select(g => g.GroupId.ToString()).ToList(),
				SpreadsheetId = task.SpreadsheetId,
			};
			var sheet = await statisticModelUtils.GetFilledGoogleSheetModel(courseStatisticsParams, 3000, UserId);
			
			var credentialsJson = configuration.GoogleAccessCredentials;
			var client = new GoogleApiClient(credentialsJson);
			client.FillSpreadSheet(courseStatisticsParams.SpreadsheetId, sheet);
			return Ok();
		}
		
		[HttpPatch("tasks/{taskId}")]
		[Authorize]
		public async Task<ActionResult> UpdateTask([FromBody] GoogleSheetsExportTaskUpdateParams param, [FromRoute] int taskId)
		{
			var task = await googleSheetExportTasksRepo.GetTaskById(taskId);
			if (task == null)
				return NotFound();
			if (!await courseRolesRepo.HasUserAccessToCourse(UserId, param.CourseId, CourseRoleType.CourseAdmin) && task.AuthorId != UserId)
				return Forbid();
			await googleSheetExportTasksRepo.UpdateTask(task,
				param.IsVisibleForStudents, param.RefreshStartDate,
				param.RefreshEndDate, param.RefreshTimeInMinutes);
			return Ok();
		}

		[HttpDelete("tasks/{taskId}")]
		[Authorize(Policy = "Instructors")]
		public async Task<ActionResult> DeleteTask([FromBody] GoogleSheetsExportTaskUpdateParams param, [FromRoute] int taskId)
		{
			var task = await googleSheetExportTasksRepo.GetTaskById(taskId);
			if (task == null)
				return NotFound();
			if (!await courseRolesRepo.HasUserAccessToCourse(UserId, param.CourseId, CourseRoleType.CourseAdmin) && task.AuthorId != UserId)
				return Forbid();
			await googleSheetExportTasksRepo.DeleteTask(task);
			return NoContent();
		}

		private async Task<bool> HasAccessToGroups(string courseId, List<int> groupsIds)
		{
			if (!await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.Instructor))
				return false;
			if (await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.CourseAdmin))
				return true;
			var accessibleGroupsIds = (await groupAccessesRepo.GetAvailableForUserGroupsAsync(courseId, UserId, true, true,true))
				.Select(g => g.Id).ToHashSet();
			return groupsIds.TrueForAll(id => accessibleGroupsIds.Contains(id));
		}
	}
}