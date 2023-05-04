using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Ulearn.Web.Api.Authorization;

public interface ICourseAuthorizationParameters
{
	string CourseId { get; set; }
}

public class CourseAuthorizationParameters : ICourseAuthorizationParameters
{
	[FromQuery(Name = "courseId")]
	[BindRequired]
	public string CourseId { get; set; }
}