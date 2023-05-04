using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ulearn.Web.Api.Models.Parameters.Groups
{
	public class CopyGroupParameters
	{
		[FromQuery(Name = "destinationCourseId")]
		[BindRequired]
		public string DestinationCourseId { get; set; }

		[FromQuery(Name = "makeMeOwner")]
		public bool ChangeOwner { get; set; } = false;
	}
}