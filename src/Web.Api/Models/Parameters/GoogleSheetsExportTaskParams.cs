using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Common.Api.Models.Validations;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Ulearn.Web.Api.Models.Parameters;

public class GoogleSheetsExportTaskUpdateParams
{
	[FromQuery(Name = "isVisibleForStudents")]
	public bool IsVisibleForStudents { get; set; }

	[FromQuery(Name = "refreshStartDate")]
	public DateTime? RefreshStartDate { get; set; }

	[FromQuery(Name = "refreshEndDate")]
	public DateTime? RefreshEndDate { get; set; }

	[FromQuery(Name = "refreshTimeInMinutes")]
	[MinValue(10, ErrorMessage = "Period should be at least 10 minutes")]
	public int RefreshTimeInMinutes { get; set; } = 60; // не чаще 10 минут,  по умолчанию час

	[FromQuery(Name = "spreadsheetId")]
	public string SpreadsheetId { get; set; }

	[FromQuery(Name = "listId")]
	public int ListId { get; set; }
}

public class GoogleSheetsCreateTaskParams : GoogleSheetsExportTaskUpdateParams
{
	[FromQuery(Name = "courseId")]
	public string CourseId { get; set; }

	[FromQuery(Name = "groupsIds")]
	public List<int> GroupsIds { get; set; } //удаленные группы
}