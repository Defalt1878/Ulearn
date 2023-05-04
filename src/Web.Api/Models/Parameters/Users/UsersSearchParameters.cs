using System.ComponentModel.DataAnnotations;
using Database.Models;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Common.Api.Models.Parameters;
using Ulearn.Common.Api.Models.Validations;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ulearn.Web.Api.Models.Parameters.Users
{
	public class UsersSearchParameters : IPaginationParameters
	{
		[FromQuery(Name = "userId")]
		public string UserId { get; set; }

		[FromQuery(Name = "query")]
		[MaxLength(100, ErrorMessage = "Query should be at most 100 chars")]
		public string Query { get; set; }

		[FromQuery(Name = "courseId")]
		public string CourseId { get; set; }

		[FromQuery(Name = "courseRole")]
		public CourseRoleType? CourseRoleType { get; set; }

		[FromQuery(Name = "lmsRole")]
		public LmsRoleType? LmsRoleType { get; set; }

		[FromQuery(Name = "offset")]
		[MinValue(0, ErrorMessage = "Offset should be non-negative")]
		[MaxValue(1000, ErrorMessage = "Offset should be at most 1000")]
		public int Offset { get; set; }

		[FromQuery(Name = "count")]
		[MinValue(0, ErrorMessage = "Count should be non-negative")]
		[MaxValue(200, ErrorMessage = "Count should be at most 200")]
		public int Count { get; set; } = 50;
	}
}