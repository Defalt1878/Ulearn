using Microsoft.AspNetCore.Mvc;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Ulearn.Web.Api.Models.Parameters.Comments
{
	public class CommentParameters
	{
		[FromQuery(Name = "withReplies")]
		public bool WithReplies { get; set; }
	}
}