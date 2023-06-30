using System;
using System.Collections.Generic;
using Database;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Common.Api.Models.Validations;

namespace Ulearn.Web.Api.Models.Parameters.CodeReview;

public class CodeReviewFilterParameters
{
	[FromQuery(Name = "studentsFilter")]
	public StudentsFilter StudentsFilter { get; set; } = StudentsFilter.MyGroups;

	[FromQuery(Name = "groupIds")]
	public List<int>? GroupIds { get; set; }

	[FromQuery(Name = "studentIds")]
	public List<string>? StudentIds { get; set; }

	[FromQuery(Name = "slideIds")]
	public Guid[]? SlideIds { get; set; }

	[FromQuery(Name = "reviewed")]
	public bool IsReviewed { get; set; } = false;

	[FromQuery(Name = "sort")]
	public DateSort DateSort { get; set; } = DateSort.Ascending;

	[FromQuery(Name = "count")]
	[MinValue(0)]
	[MaxValue(1000)]
	public int Count { get; set; } = 500;
}

public enum StudentsFilter
{
	All,
	MyGroups,
	GroupIds,
	StudentIds
}