using System;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Common.Api.Models.Parameters;
using Ulearn.Common.Api.Models.Validations;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ulearn.Web.Api.Models.Parameters.Exercise;

public class LikedAcceptedSolutionsParameters : IPaginationParameters
{
	[FromQuery(Name = "courseId")]
	public string CourseId { get; set; }

	[FromQuery(Name = "slideId")]
	public Guid SlideId { get; set; }

	[FromQuery(Name = "offset")]
	[MaxValue(100, ErrorMessage = "Offset should be at most 100")]
	public int Offset { get; set; }

	[FromQuery(Name = "count")]
	[MinValue(0, ErrorMessage = "Count should be non-negative")]
	[MaxValue(50, ErrorMessage = "Count should be at most 50")]
	public int Count { get; set; }
}