using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Ulearn.Web.Api.Models.Parameters.Analytics;

public class StatisticsParams
{
	[FromQuery(Name = "courseId")]
	public string CourseId { get; set; }
}

public class CourseStatisticsParams : StatisticsParams
{
	[FromQuery(Name = "groupsIds")]
	public List<string> GroupsIds { get; set; }

	[FromQuery(Name = "spreadsheetId")]
	public string SpreadsheetId { get; set; }

	[FromQuery(Name = "listId")]
	public int ListId { get; set; }
}