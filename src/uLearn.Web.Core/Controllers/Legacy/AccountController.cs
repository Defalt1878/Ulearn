﻿using System.ComponentModel.DataAnnotations;
using System.Net;
using AngleSharp.Common;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.SystemAccessesRepo;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Common.Extensions;
using Ulearn.Core.Configuration;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Manager;
using uLearn.Web.Core.Controllers;
using uLearn.Web.Core.Extensions;
using uLearn.Web.Core.Models;
using Vostok.Logging.Abstractions;
using Web.Api.Configuration;
using Group = System.Text.RegularExpressions.Group;


public class ULearnAuthorizeAttribute : AuthorizeAttribute
{
	public CourseRoleType MinAccessLevel = CourseRoleType.Student;

	// protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
	// {
	// 	var request = filterContext.HttpContext.Request;
	// 	if (request.HttpMethod == "POST" && request.UrlReferrer != null)
	// 	{
	// 		filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { action = "Index", controller = "Login", returnUrl = request.UrlReferrer.PathAndQuery }));
	// 		return;
	// 	}
	//
	// 	base.HandleUnauthorizedRequest(filterContext);
	// }

	// protected override bool AuthorizeCore(HttpContextBase httpContext)
	// {
	// 	if (httpContext == null)
	// 		throw new ArgumentNullException(nameof(httpContext));
	//
	// 	var user = httpContext.User;
	// 	if (!user.Identity.IsAuthenticated)
	// 		return false;
	//
	// 	var userId = httpContext.User.Identity.GetUserId();
	// 	var usersRepo = new UsersRepo(new ULearnDb());
	// 	if (usersRepo.FindUserById(userId) == null) // I.e. if user has been deleted
	// 		return false;
	//
	// 	if (MinAccessLevel == CourseRole.Student && !ShouldBeSysAdmin)
	// 		return true;
	//
	// 	if (ShouldBeSysAdmin && user.IsSystemAdministrator())
	// 		return true;
	//
	// 	if (MinAccessLevel == CourseRole.Student)
	// 		return false;
	//
	// 	var courseIds = httpContext.Request.Unvalidated.QueryString.GetValues("courseId");
	// 	if (courseIds == null)
	// 		return user.HasAccess(MinAccessLevel);
	//
	// 	if (courseIds.Length != 1)
	// 		return false;
	//
	// 	return user.HasAccessFor(courseIds[0], MinAccessLevel);
	// }

	public bool ShouldBeSysAdmin { get; set; }

	public new string Users
	{
		set { throw new NotSupportedException(); }
	}

	private static readonly char[] delims = { ',' };

	private static string[] SplitString(string original)
	{
		if (String.IsNullOrEmpty(original))
		{
			return new string[0];
		}

		return original
			.Split(delims)
			.Select(piece => piece.Trim())
			.Where(s => !String.IsNullOrEmpty(s))
			.ToArray();
	}
}

namespace uLearn.Web.Core.Controllers
{
	[ULearnAuthorize]
	public class AccountController : BaseUserController
	{
		private readonly ICourseStorage courseStorage = CourseManager.CourseStorageInstance;

		//	private readonly UserRolesRepo userRolesRepo;
		private readonly IGroupsRepo groupsRepo;
		private readonly ICertificatesRepo certificatesRepo;
		private readonly IVisitsRepo visitsRepo;
		private readonly INotificationsRepo notificationsRepo;
		private readonly ICoursesRepo coursesRepo;
		private readonly ISystemAccessesRepo systemAccessesRepo;
		private readonly ITempCoursesRepo tempCoursesRepo;
		private readonly ISlideCheckingsRepo slideCheckingsRepo;

		private readonly string telegramSecret;
		private static readonly WebApiConfiguration configuration;

		private static readonly List<string> hijackCookies = new List<string>();
		private static ILog log => LogProvider.Get().ForContext(typeof(AccountController));

		static AccountController()
		{
			configuration = ApplicationConfiguration.Read<WebApiConfiguration>();
			hijackCookies.Add(configuration.Web.CookieName);
		}

		public AccountController(
			IGroupsRepo groupsRepo, 
			ICertificatesRepo certificatesRepo, 
			IVisitsRepo visitsRepo,
			INotificationsRepo notificationsRepo,
			ICoursesRepo coursesRepo,
			ISystemAccessesRepo systemAccessesRepo,
			ITempCoursesRepo tempCoursesRepo,
			ISlideCheckingsRepo slideCheckingsRepo) :base()
		{
			//userRolesRepo = new UserRolesRepo(db);
			this.groupsRepo = groupsRepo;
			this.certificatesRepo = certificatesRepo;
			this.visitsRepo = visitsRepo;
			this.notificationsRepo = notificationsRepo;
			this.coursesRepo = coursesRepo;
			this.systemAccessesRepo = systemAccessesRepo;
			this.tempCoursesRepo = tempCoursesRepo;
			this.slideCheckingsRepo = slideCheckingsRepo;

			//    <add key="ulearn.telegram.webhook.secret" value="123" />
			telegramSecret = "123"; // WebConfigurationManager.AppSettings["ulearn.telegram.webhook.secret"] ?? "";
		}

		[AllowAnonymous]
		public ActionResult Login(string returnUrl)
		{
			return RedirectToAction("Index", "Login", new { returnUrl });
		}

		[ULearnAuthorize(MinAccessLevel = CourseRoleType.Instructor)]
		public ActionResult List(UserSearchQueryModel queryModel)
		{
			return View(queryModel);
		}

		//[ChildActionOnly]
		public async Task<ActionResult> ListPartial(UserSearchQueryModel queryModel)
		{
			var userRolesByEmail = User.IsSystemAdministrator() ? usersRepo.FilterUsersByEmail(queryModel) : null;
			var userRoles = usersRepo.FilterUsers(queryModel);
			var model = await GetUserListModel(userRolesByEmail.EmptyIfNull().Concat(userRoles).Deprecated_DistinctBy(r => r.UserId).ToList());

			return PartialView("_UserListPartial", model);
		}

		private async Task<UserListModel> GetUserListModel(List<UserRolesInfo> users)
		{
			var coursesForUsers = userRolesRepo.GetCoursesForUsers();

			var courses = User.GetControllableCoursesId().ToList();

			var currentUserId = User.Identity.GetUserId();
			var userIds = users.Select(u => u.UserId).ToList();
			var allTempCourses = tempCoursesRepo.GetAllTempCourses() // Все, потому что старые могут быть еще в памяти.
				.ToDictionary(t => t.CourseId, t => t, StringComparer.InvariantCultureIgnoreCase);
			var model = new UserListModel
			{
				CanToggleRoles = User.HasAccess(CourseRole.CourseAdmin),
				ShowDangerEntities = User.IsSystemAdministrator(),
				Users = users.Select(user => GetUserModel(user, coursesForUsers, courses, allTempCourses)).ToList(),
				UsersGroups = groupsRepo.GetUsersGroupsNamesAsStrings(courses, userIds, User, actual: true, archived: false),
				UsersArchivedGroups = groupsRepo.GetUsersGroupsNamesAsStrings(courses, userIds, User, actual: false, archived: true),
				CanViewAndToggleCourseAccesses = false,
				CanViewAndToogleSystemAccesses = User.IsSystemAdministrator(),
				CanViewProfiles = systemAccessesRepo.HasSystemAccess(currentUserId, SystemAccessType.ViewAllProfiles) || User.IsSystemAdministrator(),
			};

			return model;
		}

		private UserModel GetUserModel(UserRolesInfo userRoles, Dictionary<string, Dictionary<CourseRole, List<string>>> coursesForUsers,
			List<string> coursesIds, Dictionary<string, TempCourse> allTempCourses)
		{
			var user = new UserModel(userRoles)
			{
				CourseRoles = new Dictionary<string, ICoursesRolesListModel>
				{
					{
						LmsRoleType.SysAdmin.ToString(),
						new SingleCourseRolesModel
						{
							HasAccess = userRoles.Roles.Contains(LmsRoleType.SysAdmin.ToString()),
							ToggleUrl = Url.Content($"~/Account/{nameof(ToggleSystemRole)}?userId={userRoles.UserId}&role={LmsRoleType.SysAdmin}"), // Url.Action is slow: https://www.jitbit.com/alexblog/263-fastest-way-to-generate-urls-in-an-aspnet-mvc-app/
						}
					}
				}
			};

			if (!coursesForUsers.TryGetValue(userRoles.UserId, out var coursesForUser))
				coursesForUser = new Dictionary<CourseRole, List<string>>();

			foreach (var role in Enum.GetValues(typeof(CourseRole)).Cast<CourseRole>().Where(roles => roles != CourseRole.Student))
			{
				user.CourseRoles[role.ToString()] = new ManyCourseRolesModel
				{
					CourseRoles = coursesIds
						.Select(s => new CourseRoleModel
						{
							Role = role,
							CourseId = s,
							VisibleCourseName = allTempCourses.ContainsKey(s)
								? allTempCourses[s].GetVisibleName(courseStorage.GetCourse(s).Title)
								: courseStorage.GetCourse(s).Title,
							HasAccess = coursesForUser.ContainsKey(role) && coursesForUser[role].Contains(s.ToLower()),
							ToggleUrl = Url.Content($"~/Account/{nameof(ToggleRole)}?courseId={s}&userId={user.UserId}&role={role}"),
							UserName = user.UserVisibleName,
						})
						.OrderBy(s => s.VisibleCourseName, StringComparer.InvariantCultureIgnoreCase)
						.ToList()
				};
			}

			var systemAccesses = systemAccessesRepo.GetSystemAccesses(user.UserId).Select(a => a.AccessType);
			user.SystemAccesses = Enum.GetValues(typeof(SystemAccessType))
				.Cast<SystemAccessType>()
				.ToDictionary(
					a => a,
					a => new SystemAccessModel
					{
						HasAccess = systemAccesses.Contains(a),
						ToggleUrl = Url.Content($"~/Account/{nameof(ToggleSystemAccess)}?userId={user.UserId}&accessType={a}"),
						UserName = user.UserVisibleName,
					}
				);

			return user;
		}

		private async Task NotifyAboutUserJoinedToGroup(Group group, string userId)
		{
			var notification = new JoinedToYourGroupNotification
			{
				Group = group,
				JoinedUserId = userId
			};
			await notificationsRepo.AddNotification(group.CourseId, notification, userId);
		}

		public async Task<ActionResult> JoinGroup(Guid hash)
		{
			var userId = User.Identity.GetUserId();
			var group = groupsRepo.FindGroupByInviteHash(hash);

			if (group != null && group.Members.Any(u => u.UserId == userId))
				return Redirect(Url.RouteUrl("Course.Slide", new { courseId = group.CourseId }));

			if (group is not { IsInviteLinkEnabled: true })
				return new HttpStatusCodeResult(HttpStatusCode.NotFound);

			if (Request.HttpMethod == "POST")
			{
				var alreadyInGroup = await groupsRepo.AddUserToGroup(group.Id, userId) == null;
				if (!alreadyInGroup)
					await NotifyAboutUserJoinedToGroup(group, userId);

				await slideCheckingsRepo.ResetManualCheckingLimitsForUser(group.CourseId, userId).ConfigureAwait(false);

				return View("JoinedToGroup", group);
			}

			return View(group);
		}

		[ULearnAuthorize(ShouldBeSysAdmin = true)]
		[ValidateAntiForgeryToken]
		[HandleHttpAntiForgeryException]
		public ActionResult ToggleSystemRole(string userId, string role)
		{
			if (userId == User.Identity.GetUserId())
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			if (userManager.IsInRole(userId, role))
				userManager.RemoveFromRole(userId, role);
			else
				userManager.AddToRole(userId, role);
			return Content(role);
		}

		private async Task NotifyAboutNewInstructor(string courseId, string userId, string initiatedUserId)
		{
			var notification = new AddedInstructorNotification
			{
				AddedUserId = userId,
			};
			await notificationsRepo.AddNotification(courseId, notification, initiatedUserId);
		}

		[ULearnAuthorize(MinAccessLevel = CourseRole.Instructor)]
		[ValidateAntiForgeryToken]
		[HandleHttpAntiForgeryException]
		public async Task<ActionResult> ToggleRole(string courseId, string userId, CourseRole role)
		{
			var comment = Request.Form["comment"];
			var currentUserId = User.Identity.GetUserId();
			var isCourseAdmin = User.HasAccessFor(courseId, CourseRole.CourseAdmin);
			if ((userManager.FindById(userId) == null || userId == currentUserId) && (!isCourseAdmin || role == CourseRole.CourseAdmin) && !User.IsSystemAdministrator())
				return Json(new { status = "error", message = "Вы не можете изменить эту роль у самих себя." });

			var canAddInstructors = coursesRepo.HasCourseAccess(currentUserId, courseId, CourseAccessType.AddAndRemoveInstructors);
			if (!isCourseAdmin && !canAddInstructors)
				return Json(new { status = "error", message = "У вас нет прав назначать преподавателей или тестеров. Это могут делать только администраторы курса и преподаватели со специальными правами." });

			if (!isCourseAdmin && role == CourseRole.CourseAdmin)
				return Json(new { status = "error", message = "Вы не можете назначать администраторов курса. Это могут делать только другие администраторы курса." });

			var enabledRole = await userRolesRepo.ToggleRole(courseId, userId, role, currentUserId, comment);

			if (enabledRole && (role == CourseRole.Instructor || role == CourseRole.CourseAdmin))
				await NotifyAboutNewInstructor(courseId, userId, currentUserId);

			return Json(new { status = "ok", role = role.ToString() });
		}

		[HttpPost]
		[ULearnAuthorize(ShouldBeSysAdmin = true)]
		[ValidateAntiForgeryToken]
		[HandleHttpAntiForgeryException]
		public async Task<ActionResult> DeleteUser(string userId)
		{
			var user = usersRepo.FindUserById(userId);
			if (user != null)
			{
				/* Log out user everywhere: https://msdn.microsoft.com/en-us/library/dn497579%28v=vs.108%29.aspx?f=255&MSPPError=-2147217396 */
				await userManager.UpdateSecurityStampAsync(userId);

				await usersRepo.DeleteUserAsync(user);
			}

			return RedirectToAction("List");
		}

		[ULearnAuthorize(MinAccessLevel = CourseRole.Instructor)]
		/* Now we use AccountController.Profile and don't use AccountController.Info, but this method exists for back compatibility */
		public ActionResult Info(string userName)
		{
			var user = db.Users.FirstOrDefault(u => (u.Id == userName || u.UserName == userName) && !u.IsDeleted);
			if (user == null)
				return HttpNotFound();

			return RedirectToAction("Profile", new { userId = user.Id });
		}

		[ULearnAuthorize(MinAccessLevel = CourseRole.Instructor)]
		public ActionResult CourseInfo(string userId, string courseId)
		{
			var user = usersRepo.FindUserById(userId);
			if (user == null)
				return RedirectToAction("List");

			var course = courseStorage.GetCourse(courseId);

			return View(new UserCourseModel(course, user, db));
		}

		[ULearnAuthorize(MinAccessLevel = CourseRole.Instructor)]
		public async Task<ActionResult> ToggleRolesHistory(string userId, string courseId)
		{
			var user = usersRepo.FindUserById(userId);
			if (user == null)
				return RedirectToAction("List");

			var course = courseStorage.GetCourse(courseId);
			var model = new UserCourseToggleHistoryModel(user, course,
				ToSingleCourseRolesHistoryModel(userRolesRepo.GetUserRolesHistoryByCourseId(userId, courseId)),
				ToSingleCourseAccessHistoryModel(coursesRepo.GetUserAccessHistoryByCourseId(userId, courseId)));
			return View(model);
		}

		public async Task<ActionResult> Profile(string userId)
		{
			var user = usersRepo.FindUserById(userId);
			if (user == null)
				return HttpNotFound();

			if (!systemAccessesRepo.HasSystemAccess(User.Identity.GetUserId(), SystemAccessType.ViewAllProfiles) && !User.IsSystemAdministrator())
				return HttpNotFound();

			var logins = await userManager.GetLoginsAsync(userId);

			var userCoursesIds = visitsRepo.GetUserCourses(user.Id).Select(s => s.ToLower());
			var courses = courseStorage.GetCourses().ToList();
			var userCourses = courses.Where(c => userCoursesIds.Contains(c.Id.ToLower())).OrderBy(c => c.Title).ToList();

			var allCourses = courses.ToDictionary(c => c.Id, c => c, StringComparer.InvariantCultureIgnoreCase);
			var allTempCourses = tempCoursesRepo.GetAllTempCourses() // Все, потому что старые могут быть еще в памяти.
				.ToDictionary(t => t.CourseId, t => t, StringComparer.InvariantCultureIgnoreCase);
			var certificates = certificatesRepo.GetUserCertificates(user.Id).OrderBy(c => allCourses.GetOrDefault(c.Template.CourseId)?.Title ?? "<курс удалён>").ToList();

			var courseGroups = userCourses.ToDictionary(c => c.Id, c => groupsRepo.GetUserGroupsNamesAsString(c.Id, userId, User, actual: true, archived: false, maxCount: 10));
			var courseArchivedGroups = userCourses.ToDictionary(c => c.Id, c => groupsRepo.GetUserGroupsNamesAsString(c.Id, userId, User, actual: false, archived: true, maxCount: 10));
			var coursesWithRoles = userRolesRepo.GetUserRolesHistory(userId).Select(x => x.CourseId.ToLower()).Distinct().ToList();
			var coursesWithAccess = coursesRepo.GetUserAccessHistory(userId).Select(x => x.CourseId.ToLower()).Distinct().ToList();

			return View(new ProfileModel
			{
				User = user,
				Logins = logins,
				UserCourses = userCourses,
				CourseGroups = courseGroups,
				CourseArchivedGroups = courseArchivedGroups,
				Certificates = certificates,
				AllCourses = allCourses,
				AllTempCourses = allTempCourses,
				CoursesWithRoles = coursesWithRoles,
				CoursesWithAccess = coursesWithAccess
			});
		}

		private List<UserToggleModel> ToSingleCourseAccessHistoryModel(List<CourseAccess> historyByCourse)
		{
			return historyByCourse.Select(a => new UserToggleModel()
			{
				IsEnabled = a.IsEnabled,
				GrantedBy = usersRepo.FindUserById(a.GrantedById).VisibleName,
				Comment = a.Comment,
				GrantTimeUtc = a.GrantTime,
				Grant = a.AccessType.GetDisplayName(),
				GrantType = GrantType.Access
			}).ToList();
		}

		private List<UserToggleModel> ToSingleCourseRolesHistoryModel(List<UserRole> historyByCourse)
		{
			return historyByCourse.Select(a => new UserToggleModel()
			{
				IsEnabled = a.IsEnabled ?? true,
				GrantedBy = a.GrantedById == null ? "" : usersRepo.FindUserById(a.GrantedById).VisibleName,
				Comment = a.Comment,
				GrantTimeUtc = a.GrantTime ?? DateTime.MinValue,
				Grant = a.Role.GetDisplayName(),
				GrantType = GrantType.Role
			}).ToList();
		}


		[AllowAnonymous]
		public ActionResult Register(string returnUrl = null)
		{
			return View(new RegistrationViewModel { ReturnUrl = returnUrl });
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateInput(false)]
		[ValidateAntiForgeryToken]
		[HandleHttpAntiForgeryException]
		public async Task<ActionResult> Register(RegistrationViewModel model)
		{
			if (ModelState.IsValid)
			{
				/* Some users enter email with trailing whitespaces. Remove them (not users, but spaces!) */
				model.Email = (model.Email ?? "").Trim();

				if (!CanNewUserSetThisEmail(model.Email))
				{
					ModelState.AddModelError("Email", ManageMessageId.EmailAlreadyTaken.GetDisplayName());
					return View(model);
				}

				var user = new ApplicationUser { UserName = model.UserName, Email = model.Email, Gender = model.Gender };
				var result = await userManager.CreateAsync(user, model.Password);
				if (result.Succeeded)
				{
					await AuthenticationManager.LoginAsync(HttpContext, user, isPersistent: true);

					if (!await SendConfirmationEmail(user))
					{
						log.Warn("Register(): can't send confirmation email");
						model.ReturnUrl = Url.Action("Manage", "Account", new { Message = ManageMessageId.ErrorOccured });
					}
					else if (string.IsNullOrWhiteSpace(model.ReturnUrl))
						model.ReturnUrl = Url.Action("Index", "Home");
					else
						model.ReturnUrl = this.FixRedirectUrl(model.ReturnUrl);

					metricSender.SendCount("registration.success");

					model.RegistrationFinished = true;
				}
				else
					this.AddErrors(result);
			}

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[HandleHttpAntiForgeryException]
		public async Task<ActionResult> Disassociate(string loginProvider, string providerKey)
		{
			var result = await userManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
			var message = result.Succeeded ? ManageMessageId.LoginRemoved : ManageMessageId.ErrorOccured;
			return RedirectToAction("Manage", new { Message = message });
		}

		public async Task<ActionResult> Manage(ManageMessageId? message, string provider = "", string otherUserId = "")
		{
			ViewBag.StatusMessage = message?.GetAttribute<DisplayAttribute>().GetName();
			ViewBag.IsStatusMessageAboutSocialLogins = message == ManageMessageId.LoginAdded || message == ManageMessageId.LoginRemoved;
			if (message == ManageMessageId.AlreadyLinkedToOtherUser)
			{
				var otherUser = await userManager.FindByIdAsync(otherUserId);
				ViewBag.StatusMessage += $" {provider ?? ""}. Аккаунт уже привязан к пользователю {otherUser?.UserName ?? ""}.";
			}

			ViewBag.IsStatusError = message?.GetAttribute<IsErrorAttribute>()?.IsError ?? IsErrorAttribute.DefaultValue;
			ViewBag.HasLocalPassword = ControllerUtils.HasPassword(userManager, User);
			ViewBag.ReturnUrl = Url.Action("Manage");
			return View();
		}

		[HttpPost]
		[ValidateInput(false)]
		[ValidateAntiForgeryToken]
		[HandleHttpAntiForgeryException]
		public async Task<ActionResult> Manage(ManageUserViewModel model)
		{
			var hasPassword = ControllerUtils.HasPassword(userManager, User);
			ViewBag.HasLocalPassword = hasPassword;
			ViewBag.ReturnUrl = Url.Action("Manage");
			if (hasPassword)
			{
				if (ModelState.IsValid)
				{
					var result = await userManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
					if (result.Succeeded)
					{
						return RedirectToAction("Manage", new { Message = ManageMessageId.PasswordChanged });
					}

					this.AddErrors(result);
				}
				else
				{
					ModelState.AddModelError("", "Есть ошибки, давай поправим");
				}
			}
			else
			{
				// User does not have a password so remove any validation errors caused by a missing OldPassword field
				var state = ModelState["OldPassword"];
				state?.Errors.Clear();

				if (ModelState.IsValid)
				{
					var result = await userManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
					if (result.Succeeded)
					{
						return RedirectToAction("Manage", new { Message = ManageMessageId.PasswordSet });
					}

					this.AddErrors(result);
				}
				else
				{
					ModelState.AddModelError("", "Есть ошибки, давай поправим");
				}
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		public async Task<ActionResult> StudentInfo()
		{
			var userId = User.Identity.GetUserId();
			var user = await userManager.FindByIdAsync(userId);
			return View(new LtiUserViewModel
			{
				FirstName = user.FirstName,
				LastName = user.LastName,
				Email = user.Email,
			});
		}

		[HttpPost]
		[ValidateInput(false)]
		[ValidateAntiForgeryToken]
		[HandleHttpAntiForgeryException]
		public async Task<ActionResult> StudentInfo(LtiUserViewModel userInfo)
		{
			var userId = User.Identity.GetUserId();
			var user = await userManager.FindByIdAsync(userId);
			user.FirstName = userInfo.FirstName;
			user.LastName = userInfo.LastName;
			user.Email = (userInfo.Email ?? "").Trim();
			user.LastEdit = DateTime.Now;
			await userManager.UpdateAsync(user);
			return RedirectToAction("StudentInfo");
		}

		[ChildActionOnly]
		public ActionResult RemoveAccountList()
		{
			var linkedAccounts = userManager.GetLogins(User.Identity.GetUserId());
			var user = userManager.FindById(User.Identity.GetUserId());

			ViewBag.User = user;
			ViewBag.ShowRemoveButton = ControllerUtils.HasPassword(userManager, User) || linkedAccounts.Count > 1;

			return PartialView("_RemoveAccountPartial", linkedAccounts);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && userManager != null)
			{
				userManager.Dispose();
				userManager = null;
			}

			base.Dispose(disposing);
		}

		public enum ManageMessageId
		{
			[Display(Name = "Пароль изменён")]
			PasswordChanged,

			[Display(Name = "Пароль установлен")]
			PasswordSet,

			[Display(Name = "Привязка удалена")]
			LoginRemoved,

			[Display(Name = "Ваша почта уже подтверждена")]
			EmailAlreadyConfirmed,

			[Display(Name = "Не получилось привязать аккаунт")]
			[IsError(true)]
			AlreadyLinkedToOtherUser,

			[Display(Name = "Мы отправили вам письмо для подтверждения адреса")]
			ConfirmationEmailSent,

			[Display(Name = "Адрес электронной почты подтверждён")]
			EmailConfirmed,

			[Display(Name = "Аккаунт telegram добавлен в ваш профиль")]
			TelegramAdded,

			[Display(Name = "Аккаунт telegram удален из вашего профиля")]
			TelegramRemoved,

			[Display(Name = "У вас не указан адрес эл. почты")]
			[IsError(true)]
			UserHasNoEmail,

			[Display(Name = "Произошла ошибка. Если она будет повторяться, напишите нам на support@ulearn.me.")]
			[IsError(true)]
			ErrorOccured,

			[Display(Name = "Аккаунт привязан")]
			LoginAdded,

			[Display(Name = "Это имя уже занято, выберите другое")]
			[IsError(true)]
			NameAlreadyTaken,

			[Display(Name = "Этот адрес электронной почты уже используется другим пользователем")]
			[IsError(true)]
			EmailAlreadyTaken,

			[Display(Name = "Не все поля заполнены верно. Проверьте, пожалуйста, и попробуйте ещё раз")]
			[IsError(true)]
			NotAllFieldsFilled
		}

		public PartialViewResult ChangeDetailsPartial()
		{
			var user = userManager.FindByName(User.Identity.Name);
			var hasPassword = ControllerUtils.HasPassword(userManager, User);

			return PartialView(new UserViewModel
			{
				Name = user.UserName,
				User = user,
				HasPassword = hasPassword,
				FirstName = user.FirstName,
				LastName = user.LastName,
				Email = user.Email,
				Gender = user.Gender,
			});
		}

		[HttpPost]
		[ValidateInput(false)]
		[ValidateAntiForgeryToken]
		[HandleHttpAntiForgeryException]
		public async Task<ActionResult> ChangeDetailsPartial(UserViewModel userModel)
		{
			if (userModel.Render)
			{
				ModelState.Clear();

				return ChangeDetailsPartial();
			}

			if (string.IsNullOrEmpty(userModel.Name))
			{
				return RedirectToAction("Manage", new { Message = ManageMessageId.NotAllFieldsFilled });
			}

			var user = await userManager.FindByIdAsync(User.Identity.GetUserId());
			if (user == null)
			{
				AuthenticationManager.Logout(HttpContext);
				return RedirectToAction("Index", "Login");
			}

			var nameChanged = user.UserName != userModel.Name;
			if (nameChanged && await userManager.FindByNameAsync(userModel.Name) != null)
			{
				log.Warn($"ChangeDetailsPartial(): name {userModel.Name} is already taken");
				return RedirectToAction("Manage", new { Message = ManageMessageId.NameAlreadyTaken });
			}

			/* Some users enter email with trailing whitespaces. Remove them (not users, but spaces!) */
			userModel.Email = (userModel.Email ?? "").Trim();
			var emailChanged = string.Compare(user.Email, userModel.Email, StringComparison.OrdinalIgnoreCase) != 0;

			if (emailChanged)
			{
				if (!CanUserSetThisEmail(user, userModel.Email))
				{
					log.Warn($"ChangeDetailsPartial(): email {userModel.Email} is already taken");
					return RedirectToAction("Manage", new { Message = ManageMessageId.EmailAlreadyTaken });
				}
			}

			user.UserName = userModel.Name;
			user.FirstName = userModel.FirstName;
			user.LastName = userModel.LastName;
			user.Email = userModel.Email;
			user.Gender = userModel.Gender;
			user.LastEdit = DateTime.Now;
			if (!string.IsNullOrEmpty(userModel.Password))
			{
				await userManager.RemovePasswordAsync(user.Id);
				await userManager.AddPasswordAsync(user.Id, userModel.Password);
			}

			await userManager.UpdateAsync(user);

			if (emailChanged)
				await ChangeEmail(user, user.Email).ConfigureAwait(false);

			if (nameChanged)
			{
				AuthenticationManager.Logout(HttpContext);
				return RedirectToAction("Index", "Login");
			}

			return RedirectToAction("Manage");
		}

		public async Task<ActionResult> RemoveTelegram()
		{
			var userId = User.Identity.GetUserId();
			await usersRepo.ChangeTelegram(userId, null, null).ConfigureAwait(false);
			var telegramTransport = notificationsRepo.FindUsersNotificationTransport<TelegramNotificationTransport>(userId);
			if (telegramTransport != null)
				await notificationsRepo.EnableNotificationTransport(telegramTransport.Id, false).ConfigureAwait(false);
			return RedirectToAction("Manage", new { Message = ManageMessageId.TelegramRemoved });
		}

		[HttpPost]
		[ULearnAuthorize(ShouldBeSysAdmin = true)]
		[ValidateAntiForgeryToken]
		[ValidateInput(false)]
		[HandleHttpAntiForgeryException]
		public async Task<ActionResult> ResetPassword(string newPassword, string userId)
		{
			var user = await userManager.FindByIdAsync(userId);
			if (user == null)
				return RedirectToAction("List");
			await userManager.RemovePasswordAsync(userId);
			await userManager.AddPasswordAsync(userId, newPassword);
			return RedirectToAction("Profile", new { userId = user.Id });
		}

		[AllowAnonymous]
		public ActionResult UserMenuPartial()
		{
			var isAuthenticated = Request.IsAuthenticated;
			var userId = User.Identity.GetUserId();
			var user = userManager.FindById(userId);
			return PartialView(new UserMenuPartialViewModel
			{
				IsAuthenticated = isAuthenticated,
				User = user,
			});
		}

		public async Task<ActionResult> AddTelegram(long chatId, string chatTitle, string hash)
		{
			metricSender.SendCount("connect_telegram.try");
			var correctHash = notificationsRepo.GetSecretHashForTelegramTransport(chatId, chatTitle, telegramSecret);
			if (hash != correctHash)
				return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

			var userId = User.Identity.GetUserId();
			await usersRepo.ChangeTelegram(userId, chatId, chatTitle).ConfigureAwait(false);
			metricSender.SendCount("connect_telegram.success");
			var telegramTransport = notificationsRepo.FindUsersNotificationTransport<TelegramNotificationTransport>(userId);
			if (telegramTransport != null)
				await notificationsRepo.EnableNotificationTransport(telegramTransport.Id).ConfigureAwait(false);
			else
			{
				await notificationsRepo.AddNotificationTransport(new TelegramNotificationTransport
				{
					UserId = userId,
					IsEnabled = true,
				}).ConfigureAwait(false);
			}

			return RedirectToAction("Manage", new { Message = ManageMessageId.TelegramAdded });
		}

		[AllowAnonymous]
		public async Task<ActionResult> ConfirmEmail(string email, string signature, string userId = "")
		{
			metricSender.SendCount("email_confirmation.go_by_link_from_email");

			var realUserId = string.IsNullOrEmpty(userId) ? User.Identity.GetUserId() : userId;
			if (string.IsNullOrEmpty(realUserId))
				return HttpNotFound();

			var correctSignature = GetEmailConfirmationSignature(email, realUserId);
			if (signature != correctSignature)
			{
				metricSender.SendCount("email_confirmation.attempt_to_hack_account"); //https://yt.skbkontur.ru/issue/WHSIyt-2443
				log.Warn($"Invalid signature in confirmation email link, expected \"{correctSignature}\", actual \"{signature}\". Email is \"{email}\",");
				return RedirectToAction("Manage", new { Message = ManageMessageId.ErrorOccured });
			}

			var user = await userManager.FindByIdAsync(realUserId).ConfigureAwait(false);
			if (!User.Identity.IsAuthenticated || User.Identity.GetUserId() != realUserId)
			{
				await AuthenticationManager.LoginAsync(HttpContext, user, isPersistent: false).ConfigureAwait(false);
			}

			if (user.Email != email || user.EmailConfirmed)
				return RedirectToAction("Manage", new { Message = ManageMessageId.EmailAlreadyConfirmed });

			/* Is there are exist other users with same confirmed email, then un-confirm their emails */
			var usersWithSameEmail = usersRepo.FindUsersByEmail(email);
			foreach (var otherUser in usersWithSameEmail)
				if (otherUser.EmailConfirmed)
					await usersRepo.ConfirmEmail(otherUser.Id, false).ConfigureAwait(false);

			await usersRepo.ConfirmEmail(realUserId).ConfigureAwait(false);
			metricSender.SendCount("email_confirmation.confirmed");

			/* Enable notification transport if it exists or create auto-enabled mail notification transport */
			var mailNotificationTransport = notificationsRepo.FindUsersNotificationTransport<MailNotificationTransport>(realUserId, includeDisabled: true);
			if (mailNotificationTransport != null)
				await notificationsRepo.EnableNotificationTransport(mailNotificationTransport.Id).ConfigureAwait(false);
			else
				await notificationsRepo.AddNotificationTransport(new MailNotificationTransport
				{
					User = user,
					IsEnabled = true,
				}).ConfigureAwait(false);

			return RedirectToAction("Manage", new { Message = ManageMessageId.EmailConfirmed });
		}

		public async Task<ActionResult> SendConfirmationEmail()
		{
			var userId = User.Identity.GetUserId();
			var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
			if (string.IsNullOrEmpty(user.Email))
				return RedirectToAction("Manage", new { Message = ManageMessageId.UserHasNoEmail });

			if (user.EmailConfirmed)
				return RedirectToAction("Manage", new { Message = ManageMessageId.EmailAlreadyConfirmed });

			if (!await SendConfirmationEmail(user).ConfigureAwait(false))
			{
				log.Warn($"SendConfirmationEmail(): can't send confirmation email to user {user}");
				return RedirectToAction("Manage", new { Message = ManageMessageId.ErrorOccured });
			}

			return RedirectToAction("Manage");
		}

		public async Task ChangeEmail(ApplicationUser user, string email)
		{
			await usersRepo.ChangeEmail(user, email).ConfigureAwait(false);

			/* Disable mail notification transport if exists */
			var mailNotificationTransport = notificationsRepo.FindUsersNotificationTransport<MailNotificationTransport>(user.Id);
			if (mailNotificationTransport != null)
				await notificationsRepo.EnableNotificationTransport(mailNotificationTransport.Id, isEnabled: false).ConfigureAwait(false);

			/* Send confirmation email to the new address */
			await SendConfirmationEmail(user).ConfigureAwait(false);
		}

		[ULearnAuthorize(ShouldBeSysAdmin = true)]
		[HttpPost]
		public async Task<ActionResult> ToggleSystemAccess(string userId, SystemAccessType accessType, bool isEnabled)
		{
			var currentUserId = User.Identity.GetUserId();
			if (isEnabled)
				await systemAccessesRepo.GrantAccess(userId, accessType, currentUserId);
			else
				await systemAccessesRepo.RevokeAccess(userId, accessType);

			return Json(new { status = "ok" });
		}

		[ULearnAuthorize(ShouldBeSysAdmin = true)]
		[HttpPost]
		public async Task<ActionResult> Hijack(string userId)
		{
			var user = await userManager.FindByIdAsync(userId);
			if (user == null)
				return HttpNotFound("User not found");

			CopyHijackedCookies(HttpContext.Request, HttpContext.Response, s => s, s => s + ".hijack", removeOld: false);
			await AuthenticationManager.LoginAsync(HttpContext, user, isPersistent: false);

			return Redirect("/");
		}

		[HttpPost]
		[AllowAnonymous]
		public ActionResult ReturnHijack()
		{
			var hijackedUserId = User.Identity.GetUserId();
			CopyHijackedCookies(HttpContext.Request, HttpContext.Response, s => s + ".hijack", s => s, removeOld: true);
			return RedirectToAction("Profile", "Account", new { userId = hijackedUserId });
		}

		private void CopyHijackedCookies(HttpRequestBase request, HttpResponseBase response, Func<string, string> actualCookie, Func<string, string> newCookie, bool removeOld)
		{
			foreach (var cookieName in hijackCookies)
			{
				var cookie = request.Cookies.Get(actualCookie(cookieName));
				if (cookie == null)
					continue;

				response.Cookies.Add(new HttpCookie(newCookie(cookieName), cookie.Value)
				{
					Domain = configuration.Web.CookieDomain,
					Secure = configuration.Web.CookieSecure
				});

				if (removeOld)
					response.Cookies.Add(new HttpCookie(actualCookie(cookieName), "")
					{
						Expires = DateTime.Now.AddDays(-1),
						Domain = configuration.Web.CookieDomain,
						Secure = configuration.Web.CookieSecure
					});
			}
		}
	}

	public class ProfileModel
	{
		public ApplicationUser User { get; set; }
		public IList<UserLoginInfo> Logins { get; set; }
		public List<Course> UserCourses { get; set; }
		public List<Certificate> Certificates { get; set; }
		public Dictionary<string, Course> AllCourses { get; set; }
		public Dictionary<string, string> CourseGroups { get; set; }
		public Dictionary<string, string> CourseArchivedGroups { get; set; }

		public Dictionary<string, TempCourse> AllTempCourses { get; set; }

		public List<string> CoursesWithRoles;

		public List<string> CoursesWithAccess;
	}


	public class IsErrorAttribute : Attribute
	{
		public static bool DefaultValue = false;

		public readonly bool IsError;

		public IsErrorAttribute(bool isError)
		{
			IsError = isError;
		}
	}
}