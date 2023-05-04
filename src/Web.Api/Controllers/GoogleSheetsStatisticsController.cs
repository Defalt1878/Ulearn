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
using Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Core.Configuration;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.GoogleSheet;
using Ulearn.Web.Api.Models.Parameters;
using Ulearn.Web.Api.Models.Parameters.Analytics;
using Ulearn.Web.Api.Models.Responses;
using Ulearn.Web.Api.Utils;
using Web.Api.Configuration;

namespace Ulearn.Web.Api.Controllers
{
	[Route("/course-statistics/export/to-google-sheets/tasks")]
	public class GoogleSheetsStatisticsController : BaseController
	{
		private readonly UlearnConfiguration configuration;
		private readonly ICourseRolesRepo courseRolesRepo;
		private readonly IGoogleSheetExportTasksRepo googleSheetExportTasksRepo;
		private readonly IGroupAccessesRepo groupAccessesRepo;
		private readonly StatisticModelUtils statisticModelUtils;

		public GoogleSheetsStatisticsController(ICourseStorage courseStorage, UlearnDb db,
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

		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			if (context.ActionArguments.TryGetValue("courseId", out var courseIdObj))
			{
				var courseId = (string)courseIdObj;

				if (!courseStorage.HasCourse(courseId))
				{
					context.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
					context.Result = new JsonResult(new ErrorResponse($"Course {courseId} not found"));
					return;
				}

				if (!await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.Instructor))
				{
					context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
					context.Result = new JsonResult(new ErrorResponse($"You don't have access to course {courseId}"));
					return;
				}
			}

			if (context.ActionArguments.TryGetValue("taskId", out var taskIdObj))
			{
				var taskId = (int) taskIdObj!;
				var task = await googleSheetExportTasksRepo.GetTaskById(taskId);
				if (task is null)
				{
					context.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
					context.Result = new JsonResult(new ErrorResponse($"Task with id {taskId} not found"));
					return;
				}

				if (!await courseRolesRepo.HasUserAccessToCourse(UserId, task.CourseId, CourseRoleType.Instructor))
				{
					context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
					context.Result = new JsonResult(new ErrorResponse($"You don't have access to course {task.CourseId}"));
					return;
				}

				var isCourseAdmin = await courseRolesRepo.HasUserAccessToCourse(UserId, task.CourseId, CourseRoleType.CourseAdmin);

				if (!isCourseAdmin && task.AuthorId != UserId)
				{
					context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
					context.Result = new JsonResult(new ErrorResponse("You don't have a permission to view this task"));
					return;
				}
			}

			await base.OnActionExecutionAsync(context, next);
		}


		[HttpGet]
		[Authorize(Policy = "Instructors")]
		public async Task<ActionResult<GoogleSheetsExportTaskListResponse>> GetAllCourseTasks([FromQuery] string courseId)
		{
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

		[HttpGet("{taskId:int}")]
		[Authorize]
		public async Task<ActionResult<GoogleSheetsExportTaskResponse>> GetTaskById([FromRoute] int taskId)
		{
			var task = await googleSheetExportTasksRepo.GetTaskById(taskId);

			var result = new GoogleSheetsExportTaskResponse
			{
				Id = task.Id,
				AuthorInfo = BuildShortUserInfo(task.Author),
				Groups = task.Groups.Select(e => BuildShortGroupInfo(e.Group)).ToList(),
				IsVisibleForStudents = task.IsVisibleForStudents,
				RefreshStartDate = task.RefreshStartDate,
				RefreshEndDate = task.RefreshEndDate,
				RefreshTimeInMinutes = task.RefreshTimeInMinutes,
				SpreadsheetId = task.SpreadsheetId,
				ListId = task.ListId,
				LastUpdateDate = task.LastUpdateDate,
				LastUpdateErrorMessage = task.LastUpdateErrorMessage
			};
			return result;
		}

		[HttpPost]
		[Authorize]
		public async Task<ActionResult<GoogleSheetsExportTaskResponse>> AddNewTask([FromBody] GoogleSheetsCreateTaskParams param)
		{
			if (!await HasAccessToGroups(param.CourseId, param.GroupsIds))
				return Forbid("You don't have access to selected groups");

			var id = await googleSheetExportTasksRepo.AddTask(param.CourseId, UserId, param.IsVisibleForStudents,
				param.RefreshStartDate, param.RefreshEndDate, param.RefreshTimeInMinutes, param.GroupsIds,
				param.SpreadsheetId, param.ListId);

			var task = await googleSheetExportTasksRepo.GetTaskById(id);

			var result = new GoogleSheetsExportTaskResponse
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
			};
			return result;
		}

		[HttpPost("{taskId:int}")]
		[Authorize]
		public async Task<ActionResult> ExportTaskNow([FromRoute] int taskId)
		{
			var task = await googleSheetExportTasksRepo.GetTaskById(taskId);

			var courseStatisticsParams = new CourseStatisticsParams
			{
				CourseId = task.CourseId,
				ListId = task.ListId,
				GroupsIds = task.Groups.Select(g => g.GroupId.ToString()).ToList(),
				SpreadsheetId = task.SpreadsheetId
			};

			string exceptionMessage = null;
			var utcNow = DateTime.UtcNow;
			try
			{
				var sheet = await statisticModelUtils.GetFilledGoogleSheetModel(courseStatisticsParams, 3000, UserId, utcNow);

				var credentialsJson = configuration.GoogleAccessCredentials;
				var client = new GoogleApiClient(credentialsJson);
				client.FillSpreadSheet(courseStatisticsParams.SpreadsheetId, sheet);
			}
			catch (GoogleApiException e)
			{
				exceptionMessage = $"Google Api ERROR: {e.Error.Code} {e.Error.Message}";
			}
			catch (Exception e)
			{
				exceptionMessage = e.ToString();
			}

			await googleSheetExportTasksRepo.SaveTaskUploadResult(task, utcNow, exceptionMessage);

			if (exceptionMessage is not null)
				return BadRequest(exceptionMessage);

			return Ok($"Task with id {taskId} successfully exported to google sheet");
		}

		[HttpPatch("{taskId:int}")]
		[Authorize]
		public async Task<ActionResult> UpdateTask([FromBody] GoogleSheetsExportTaskUpdateParams param, [FromRoute] int taskId)
		{
			var task = await googleSheetExportTasksRepo.GetTaskById(taskId);

			await googleSheetExportTasksRepo.UpdateTask(task,
				param.IsVisibleForStudents, param.RefreshStartDate,
				param.RefreshEndDate, param.RefreshTimeInMinutes,
				param.SpreadsheetId, param.ListId);

			return Ok($"Task {taskId} successfully updated");
		}

		[HttpDelete("{taskId:int}")]
		[Authorize]
		public async Task<ActionResult> DeleteTask([FromRoute] int taskId)
		{
			var task = await googleSheetExportTasksRepo.GetTaskById(taskId);

			await googleSheetExportTasksRepo.DeleteTask(task);

			return NoContent();
		}

		private async Task<bool> HasAccessToGroups(string courseId, List<int> groupsIds)
		{
			if (!await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.Instructor))
				return false;
			if (await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.CourseAdmin))
				return true;
			var accessibleGroupsIds = (await groupAccessesRepo.GetAvailableForUserGroupsAsync(courseId, UserId, true, true, true, GroupQueryType.All))
				.Select(g => g.Id).ToHashSet();
			return groupsIds.TrueForAll(id => accessibleGroupsIds.Contains(id));
		}
	}
}