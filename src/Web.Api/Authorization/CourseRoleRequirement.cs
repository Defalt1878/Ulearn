using System.Threading.Tasks;
using Database.Models;
using Database.Repos;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Ulearn.Common.Extensions;
using Vostok.Logging.Abstractions;

namespace Ulearn.Web.Api.Authorization;

public class CourseRoleRequirement : IAuthorizationRequirement
{
	public readonly CourseRoleType minCourseRoleType;

	public CourseRoleRequirement(CourseRoleType minCourseRoleType)
	{
		this.minCourseRoleType = minCourseRoleType;
	}
}

public class CourseRoleAuthorizationHandler : BaseCourseAuthorizationHandler<CourseRoleRequirement>
{
	private readonly ICourseRolesRepo courseRolesRepo;
	private readonly IUsersRepo usersRepo;

	public CourseRoleAuthorizationHandler(ICourseRolesRepo courseRolesRepo, IUsersRepo usersRepo)
	{
		this.courseRolesRepo = courseRolesRepo;
		this.usersRepo = usersRepo;
	}

	private static ILog Log => LogProvider.Get().ForContext(typeof(CourseRoleAuthorizationHandler));

	protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CourseRoleRequirement requirement)
	{
		/* Get MVC context. See https://docs.microsoft.com/en-US/aspnet/core/security/authorization/policies#accessing-mvc-request-context-in-handlers */
		if (context.Resource is not AuthorizationFilterContext mvcContext)
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

		if (await courseRolesRepo.HasUserAccessToCourse(userId, courseId, requirement.minCourseRoleType).ConfigureAwait(false))
			context.Succeed(requirement);
		else
			context.Fail();
	}
}