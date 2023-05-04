using Microsoft.AspNetCore.Mvc;
using Ulearn.Common.Api.Models.Parameters;
using Ulearn.Common.Api.Models.Validations;

namespace Ulearn.Web.Api.Models.Parameters.Comments;

public class CommentLikesParameters : IPaginationParameters
{
	[FromQuery(Name = "offset")]
	[MinValue(0, ErrorMessage = "Offset should be non-negative")]
	public int Offset { get; set; }

	[FromQuery(Name = "count")]
	[MinValue(0, ErrorMessage = "Count should be non-negative")]
	[MaxValue(200, ErrorMessage = "Count should be at most 200")]
	public int Count { get; set; } = 100;
}