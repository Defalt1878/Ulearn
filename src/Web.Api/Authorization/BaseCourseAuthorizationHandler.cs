using System;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Vostok.Logging.Abstractions;

namespace Ulearn.Web.Api.Authorization
{
	public class BaseCourseAuthorizationHandler<T> : AuthorizationHandler<T> where T : IAuthorizationRequirement
	{
		private static ILog Log => LogProvider.Get().ForContext(typeof(BaseCourseAuthorizationHandler<T>));

		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, T requirement)
		{
			throw new NotImplementedException();
		}

		/* Find `course_id` arguments in request. Try to get course_id in following order:
		* route data (/groups/<course_id>/)
		* query string (/groups/?course_id=<course_id>)
		* NOTE: not supported JSON request body})
		*/
		protected string GetCourseIdFromRequestAsync(AuthorizationFilterContext mvcContext)
		{
			/* 1. Route data */
			var routeData = mvcContext.RouteData;
			if (routeData.Values["courseId"] is string courseIdFromRoute)
				return courseIdFromRoute;

			var courseIdFromQuery = mvcContext.HttpContext.Request.Query["courseId"].FirstOrDefault();
			if (!courseIdFromQuery.IsNullOrEmpty())
				return courseIdFromQuery;

			Log.Error("Can't find `courseId` parameter in request for checking course role requirement. You should inherit your parameters models from ICourseAuthorizationParameters.");
			return null;
		}
	}
}