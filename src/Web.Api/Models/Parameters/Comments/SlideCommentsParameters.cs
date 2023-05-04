using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Ulearn.Common.Api.Models.Parameters;
using Ulearn.Common.Api.Models.Validations;
using Ulearn.Web.Api.Authorization;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ulearn.Web.Api.Models.Parameters.Comments;

public class SlideCommentsParameters : IPaginationParameters, ICourseAuthorizationParameters
{
	[FromQuery(Name = "slideId")]
	[BindRequired]
	public Guid SlideId { get; set; }

	[FromQuery(Name = "forInstructors")]
	public bool ForInstructors { get; set; }

	[FromQuery(Name = "courseId")]
	[BindRequired]
	public string CourseId { get; set; }

	[FromQuery(Name = "offset")]
	[MinValue(0, ErrorMessage = "Offset should be non-negative")]
	public int Offset { get; set; }

	[FromQuery(Name = "count")]
	[MinValue(0, ErrorMessage = "Count should be non-negative")]
	[MaxValue(200, ErrorMessage = "Count should be at most 200")]
	public int Count { get; set; } = 100;
}