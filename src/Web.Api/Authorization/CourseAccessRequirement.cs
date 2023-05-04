using System.Threading.Tasks;
using Database.Models;
using Database.Repos;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Ulearn.Common.Extensions;
using Vostok.Logging.Abstractions;

namespace Ulearn.Web.Api.Authorization
{
	public class CourseAccessRequirement : IAuthorizationRequirement
	{
		public readonly CourseAccessType CourseAccessType;

		public CourseAccessRequirement(CourseAccessType courseAccessType)
		{
			CourseAccessType = courseAccessType;
		}
	}

	public class CourseAccessAuthorizationHandler : BaseCourseAuthorizationHandler<CourseAccessRequirement>
	{
		private readonly ICourseRolesRepo courseRolesRepo;
		private readonly ICoursesRepo coursesRepo;
		private readonly IUsersRepo usersRepo;

		public CourseAccessAuthorizationHandler(ICoursesRepo coursesRepo, ICourseRolesRepo courseRolesRepo, IUsersRepo usersRepo)
		{
			this.coursesRepo = coursesRepo;
			this.courseRolesRepo = courseRolesRepo;
			this.usersRepo = usersRepo;
		}

		private static ILog Log => LogProvider.Get().ForContext(typeof(CourseAccessAuthorizationHandler));

		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CourseAccessRequirement requirement)
		{
			/* Get MVC context. See https://docs.microsoft.com/en-US/aspnet/core/security/authorization/policies#accessing-mvc-request-context-in-handlers */
			if (!(context.Resource is AuthorizationFilterContext mvcContext))
			{
				Log.Error("Can't get MVC context in CourseRoleAuthenticationHandler");
				context.Fail();
				return;
			}

			var courseId = GetCourseIdFromRequestAsync(mvcContext);
			if (string.IsNullOrEmpty(courseId))
			{
				context.Fail();
				return;
			}

			if (context.User.Identity is not { IsAuthenticated: true })
			{
				context.Fail();
				return;
			}

			var userId = context.User.GetUserId();
			var user = await usersRepo.FindUserById(userId).ConfigureAwait(false);
			if (user is null)
			{
				context.Fail();
				return;
			}

			if (usersRepo.IsSystemAdministrator(user))
			{
				context.Succeed(requirement);
				return;
			}

			var isCourseAdmin = await courseRolesRepo.HasUserAccessToCourse(userId, courseId, CourseRoleType.CourseAdmin).ConfigureAwait(false);
			if (isCourseAdmin || await coursesRepo.HasCourseAccess(userId, courseId, requirement.CourseAccessType).ConfigureAwait(false))
				context.Succeed(requirement);
			else
				context.Fail();
		}
	}

	public class CourseAccessAuthorizeAttribute : AuthorizeAttribute
	{
		public CourseAccessAuthorizeAttribute(CourseAccessType accessType)
			: base(accessType.GetAuthorizationPolicyName())
		{
		}
	}
}