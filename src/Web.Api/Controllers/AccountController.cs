using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.SystemAccessesRepo;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Authorization;
using Ulearn.Web.Api.Models.Parameters;
using Ulearn.Web.Api.Models.Responses.Account;
using Web.Api.Configuration;

namespace Ulearn.Web.Api.Controllers
{
	[Route("/account")]
	public class AccountController : BaseController
	{
		private readonly WebApiConfiguration configuration;
		private readonly ICourseRolesRepo courseRolesRepo;
		private readonly ICoursesRepo coursesRepo;
		private readonly IGroupMembersRepo groupMembersRepo;
		private readonly SignInManager<ApplicationUser> signInManager;
		private readonly ISystemAccessesRepo systemAccessesRepo;
		private readonly IUnitsRepo unitsRepo;
		private readonly UlearnUserManager userManager;

		public AccountController(IOptions<WebApiConfiguration> options, ICourseStorage courseStorage, UlearnDb db,
			UlearnUserManager userManager, SignInManager<ApplicationUser> signInManager,
			ICourseRolesRepo courseRolesRepo, ICoursesRepo coursesRepo, IUsersRepo usersRepo, ISystemAccessesRepo systemAccessesRepo, IGroupMembersRepo groupMembersRepo,
			IUnitsRepo unitsRepo)
			: base(courseStorage, db, usersRepo)
		{
			this.userManager = userManager;
			this.signInManager = signInManager;
			this.courseRolesRepo = courseRolesRepo;
			this.coursesRepo = coursesRepo;
			this.systemAccessesRepo = systemAccessesRepo;
			this.groupMembersRepo = groupMembersRepo;
			this.unitsRepo = unitsRepo;
			configuration = options.Value;
		}

		/// <summary>
		///     Информация о текущем пользователе
		/// </summary>
		[HttpGet]
		[Authorize]
		public async Task<ActionResult<GetMeResponse>> Me()
		{
			var user = await userManager.FindByIdAsync(UserId).ConfigureAwait(false);
			var systemAccesses = await systemAccessesRepo.GetSystemAccessesAsync(UserId).ConfigureAwait(false);
			return new GetMeResponse
			{
				IsAuthenticated = true,
				User = BuildShortUserInfo(user, true),
				AccountProblems = await GetAccountProblems(user).ConfigureAwait(false),
				SystemAccesses = systemAccesses.Select(a => a.AccessType).ToList()
			};
		}

		private async Task<List<AccountProblem>> GetAccountProblems(ApplicationUser user)
		{
			var problems = new List<AccountProblem>();
			if (string.IsNullOrEmpty(user.Email))
				problems.Add(new AccountProblem(
					"Не указана эл. почта",
					"Укажите в профиле электронную почту, чтобы получать уведомления и восстановить доступ в случае утери пароля",
					"noEmail"
				));
			if (!string.IsNullOrEmpty(user.Email) && !user.EmailConfirmed)
				problems.Add(new AccountProblem(
					"Эл. почта не подтверждена",
					"Подтвердите в профиле электронную почту, чтобы получать уведомления и восстановить доступ в случае утери пароля",
					"emailNotConfirmed"
				));

			var isInstructor = await courseRolesRepo.HasUserAccessTo_Any_Course(user.Id, CourseRoleType.Instructor).ConfigureAwait(false);
			if (isInstructor && (string.IsNullOrEmpty(user.FirstName) || string.IsNullOrEmpty(user.LastName)))
				problems.Add(new AccountProblem(
					"Не указаны имя или фамилия",
					"Укажите в профиле имя и фамилию, чтобы студентам было проще с вами работать",
					"noName"
				));

			return problems;
		}

		/// <summary>
		///     Получить JWT-токен по кукам
		/// </summary>
		[HttpPost("token")]
		[Authorize(AuthenticationSchemes = "Identity.Application" /* = IdentityConstants.ApplicationScheme */)]
		public ActionResult<TokenResponse> Token()
		{
			var claims = User.Claims.ToList();
			var expires = DateTime.Now.AddHours(configuration.Web.Authentication.Jwt.LifeTimeHours);
			return GetTokenInternal(expires, claims);
		}

		/// <summary>
		///     Получить ключ на пользователя на заданный срок в днях
		/// </summary>
		[HttpPost("api-token")]
		[Authorize]
		public async Task<ActionResult<TokenResponse>> ApiToken([FromQuery] int days)
		{
			var isInstructor = await courseRolesRepo.HasUserAccessTo_Any_Course(User.GetUserId(), CourseRoleType.Instructor).ConfigureAwait(false);
			if (!isInstructor)
				return StatusCode((int)HttpStatusCode.Forbidden, "You should be at least instructor");

			var expires = DateTime.Now.AddDays(days);
			var claims = User.Claims.ToList();
			return GetTokenInternal(expires, claims);
		}

		private TokenResponse GetTokenInternal(DateTime expires, IList<Claim> claims)
		{
			var key = JwtBearerHelpers.CreateSymmetricSecurityKey(configuration.Web.Authentication.Jwt.IssuerSigningKey);
			var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				configuration.Web.Authentication.Jwt.Issuer,
				configuration.Web.Authentication.Jwt.Audience,
				claims,
				expires: expires,
				signingCredentials: signingCredentials
			);
			var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
			return new TokenResponse
			{
				Token = tokenString
			};
		}

		/// <summary>
		///     Получить JWT-токен по логину-паролю
		/// </summary>
		[HttpPost("login")]
		public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginPasswordParameters loginPassword)
		{
			var appUser = await db.Users.FirstOrDefaultAsync(u => u.UserName == loginPassword.Login && !u.IsDeleted);
			if (appUser is null)
				return Forbid();
			var result = await userManager.CheckPasswordAsync(appUser, loginPassword.Password);
			if (!result)
				return Forbid();

			var key = JwtBearerHelpers.CreateSymmetricSecurityKey(configuration.Web.Authentication.Jwt.IssuerSigningKey);
			var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
			var expires = DateTime.Now.AddHours(configuration.Web.Authentication.Jwt.LifeTimeHours);

			var token = new JwtSecurityToken(
				configuration.Web.Authentication.Jwt.Issuer,
				configuration.Web.Authentication.Jwt.Audience,
				new[] { new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", appUser.Id) },
				expires: expires,
				signingCredentials: signingCredentials
			);
			var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
			return new TokenResponse
			{
				Token = tokenString
			};
		}

		/// <summary>
		///     Список ролей («курс-админ», «преподаватель», «тестер») текущего пользователя
		/// </summary>
		[HttpGet("roles")]
		[Authorize]
		public async Task<ActionResult<CourseRolesResponse>> CourseRoles()
		{
			var userId = User.GetUserId();
			var isSystemAdministrator = await usersRepo.IsSystemAdministrator(userId);
			var visibleCourses = await unitsRepo.GetVisibleCourses();

			var rolesByCourse = (await courseRolesRepo.GetRoles(userId).ConfigureAwait(false))
				.Where(kvp => kvp.Value != CourseRoleType.Student).ToList();
			var courseAccesses = await coursesRepo.GetUserAccesses(userId).ConfigureAwait(false);
			var courseAccessesByCourseId = courseAccesses.GroupBy(a => a.CourseId).Select(
				g => new CourseAccessResponse
				{
					CourseId = g.Key,
					Accesses = g.Select(a => a.AccessType).ToList()
				}
			).ToList();
			var coursesInWhichUserHasAnyRole = new HashSet<string>(rolesByCourse.Select(kvp => kvp.Key), StringComparer.OrdinalIgnoreCase);
			var groupsWhereIAmStudent = (await groupMembersRepo.GetUserGroupsAsync(userId).ConfigureAwait(false))
				.Where(g => visibleCourses.Contains(g.CourseId) || coursesInWhichUserHasAnyRole.Contains(g.CourseId) || isSystemAdministrator);
			return new CourseRolesResponse
			{
				IsSystemAdministrator = isSystemAdministrator,
				CourseRoles = rolesByCourse.Select(kvp => new CourseRoleResponse
				{
					CourseId = kvp.Key,
					Role = kvp.Value
				}).ToList(),
				CourseAccesses = courseAccessesByCourseId,
				GroupsAsStudent = groupsWhereIAmStudent.Where(g => g.CanUsersSeeGroupProgress).Select(BuildShortGroupInfo).ToList()
			};
		}

		/// <summary>
		///     Выход
		/// </summary>
		[HttpPost("logout")]
		[Authorize]
		public async Task<ActionResult<LogoutResponse>> Logout()
		{
			await signInManager.SignOutAsync().ConfigureAwait(false);

			return new LogoutResponse
			{
				Logout = true
			};
		}
	}
}