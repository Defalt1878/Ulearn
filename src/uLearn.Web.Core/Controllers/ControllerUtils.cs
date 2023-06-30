using System.Security.Claims;
using System.Security.Principal;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Vostok.Logging.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Common.Extensions;
using Ulearn.Core.Configuration;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Courses.Slides.Quizzes;
using uLearn.Web.Core.Extensions;
using Web.Api.Configuration;

namespace uLearn.Web.Core.Controllers;

public static class ControllerUtils
{
	private static readonly string ulearnBaseUrl;
	private static ILog log => LogProvider.Get().ForContext(typeof(ControllerUtils));


	static ControllerUtils()
	{
		var configuration = ApplicationConfiguration.Read<WebConfiguration>();
		ulearnBaseUrl = configuration.BaseUrl ?? "";
	}

	public static async Task<bool> HasPassword(UlearnUserManager userManager, string userId)
	{
		var user = await userManager.FindByIdAsync(userId);
		return user?.PasswordHash != null;
	}

	private static bool IsLocalUrl(this Controller controller, string url)
	{
		if (controller.Url.IsLocalUrl(url))
			return true;

		if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(ulearnBaseUrl))
			return false;

		try
		{
			var ulearnBaseUrlBuilder = new UriBuilder(ulearnBaseUrl);
			var urlBuilder = new UriBuilder(url);
			return ulearnBaseUrlBuilder.Host == urlBuilder.Host && ulearnBaseUrlBuilder.Scheme == urlBuilder.Scheme && ulearnBaseUrlBuilder.Port == urlBuilder.Port;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static string FixRedirectUrl(this Controller controller, string url)
	{
		var isLocalUrl = controller.IsLocalUrl(url);

		log.Info($"Redirect to {url}: {(isLocalUrl ? "it's safe" : "it's not safe, redirect to home page")}. Base url is {ulearnBaseUrl}");
		return isLocalUrl ? url : controller.Url.Action("Index", "Home");
	}

	public static void AddErrors(this Controller controller, IdentityResult result)
	{
		foreach (var error in result.Errors)
			controller.ModelState.AddModelError("", error.Description);
	}

	public static async Task<T> GetFilterOptionsByGroup<T>(
		IGroupsRepo groupsRepo,
		IGroupAccessesRepo groupAccessesRepo,
		ClaimsPrincipal User,
		string courseId,
		List<string> groupsIds,
		bool allowSeeGroupForAnyMember = false
	) where T : AbstractFilterOptionByCourseAndUsers, new()
	{
		var result = new T { CourseId = courseId };

		/* if groupsIds contains "all" (it should be exclusive), get all users. Available only for course admins */
		if (groupsIds.Contains("all") && User.HasAccessFor(courseId, CourseRoleType.CourseAdmin))
			return result;
		/* if groupsIds contains "not-group" (it should be exclusive), get all users not in any groups, available only for course admins */
		if (groupsIds.Contains("not-in-group") && User.HasAccessFor(courseId, CourseRoleType.CourseAdmin))
		{
			var usersInGroups = await groupsRepo.GetUsersIdsForAllGroups(courseId);
			result.UserIds = usersInGroups;
			result.IsUserIdsSupplement = true;
			return result;
		}

		result.UserIds = new List<string>();

		/* if groupsIds is empty, get members of all groups user has access to. Available for instructors */
		if ((groupsIds.Count == 0 || groupsIds.Any(string.IsNullOrEmpty)) && User.HasAccessFor(courseId, CourseRoleType.Instructor))
		{
			var accessibleGroupsIds = (await groupsRepo.GetMyGroupsFilterAccessibleToUser(courseId, User.GetUserId())).Select(g => g.Id).ToList();
			result.UserIds = await groupsRepo.GetGroupsMembersAsUserIds(accessibleGroupsIds);
			return result;
		}

		var usersIds = new HashSet<string>();
		var groupsIdsInts = groupsIds.Select(s => int.TryParse(s, out var i) ? i : (int?)null).Where(i => i.HasValue).Select(i => i.Value).ToList();
		var group2GroupMembersIds = (await groupsRepo.GetGroupsMembersAsGroupsIdsAndUserIds(groupsIdsInts))
			.GroupBy(u => u.GroupId)
			.ToDictionary(g => g.Key, g => g.Select(p => p.UserId).ToList());
		foreach (var groupIdInt in groupsIdsInts)
		{
			if (!group2GroupMembersIds.ContainsKey(groupIdInt))
				continue;
			var hasAccessToGroup = await groupAccessesRepo.IsGroupVisibleForUserAsync(groupIdInt, User.GetUserId()); //groupsRepo.IsGroupAvailableForUser(groupIdInt, User);
			if (allowSeeGroupForAnyMember)
				hasAccessToGroup |= group2GroupMembersIds[groupIdInt].Contains(User.GetUserId());
			if (hasAccessToGroup)
				usersIds.UnionWith(group2GroupMembersIds[groupIdInt]);
		}

		result.UserIds = usersIds.ToList();
		return result;
	}

	public static async Task<List<string>> GetEnabledAdditionalScoringGroupsForGroups(IGroupsRepo groupsRepo, Course course, List<string> groupsIds, ClaimsPrincipal User)
	{
		if (groupsIds.Contains("all") || groupsIds.Contains("not-in-group"))
			return course.Settings.Scoring.Groups.Keys.ToList();

		var enabledAdditionalScoringGroupsForGroups = (await groupsRepo.GetEnabledAdditionalScoringGroupsAsync(course.Id))
			.GroupBy(e => e.GroupId)
			.ToDictionary(g => g.Key, g => g.Select(e => e.ScoringGroupId).ToList());

		/* if groupsIds is empty, get members of all own groups. Available for instructors */
		if (groupsIds.Count == 0 || groupsIds.Any(string.IsNullOrEmpty))
		{
			var accessibleGroupsIds = (await groupsRepo.GetMyGroupsFilterAccessibleToUser(course.Id, User.GetUserId())).Select(g => g.Id).ToList();
			return enabledAdditionalScoringGroupsForGroups.Where(kv => accessibleGroupsIds.Contains(kv.Key)).SelectMany(kv => kv.Value).ToList();
		}

		var result = new List<string>();
		foreach (var groupId in groupsIds)
		{
			int groupIdInt;
			if (int.TryParse(groupId, out groupIdInt))
				result.AddRange(enabledAdditionalScoringGroupsForGroups.GetOrDefault(groupIdInt, new List<string>()));
		}

		return result;
	}

	public static async Task<HashSet<Guid>> GetSolvedSlides(UserSolutionsRepo solutionsRepo, UserQuizzesRepo userQuizzesRepo, Course course, string userId)
	{
		var solvedSlides = await solutionsRepo.GetIdOfPassedSlides(course.Id, userId);
		solvedSlides.UnionWith(await userQuizzesRepo.GetPassedSlideIdsAsync(course.Id, userId));
		return solvedSlides;
	}

	public static async Task<bool> IsSlideSolved(ISlideCheckingsRepo slideCheckingsRepo, Course course, string userId, Guid slideId)
	{
		return await slideCheckingsRepo.IsSlidePassed(course.Id, slideId, userId);
	}

	public static int GetMaxScoreForUsersSlide(Slide slide, bool isSolved, bool hasManualChecking, bool enabledManualCheckingForUser)
	{
		var isExerciseOrQuiz = slide is ExerciseSlide || slide is QuizSlide;

		if (!isExerciseOrQuiz)
			return slide.MaxScore;

		if (isSolved)
			return hasManualChecking ? slide.MaxScore : GetMaxScoreWithoutManualChecking(slide);
		else
			return enabledManualCheckingForUser ? slide.MaxScore : GetMaxScoreWithoutManualChecking(slide);
	}

	private static int GetMaxScoreWithoutManualChecking(Slide slide)
	{
		if (slide is ExerciseSlide)
			return (slide as ExerciseSlide).Scoring.PassedTestsScore;
		if (slide is QuizSlide)
			return (slide as QuizSlide).ManualChecking ? 0 : slide.MaxScore;
		return slide.MaxScore;
	}

	public static async Task<int> GetManualCheckingsCountInQueue(ISlideCheckingsRepo slideCheckingsRepo, IGroupsRepo groupsRepo, IGroupAccessesRepo groupAccessesRepo, ClaimsPrincipal user,
		string courseId, Slide slide, List<string> groupsIds)
	{
		var filterOptions = await GetFilterOptionsByGroup<ManualCheckingQueueFilterOptionsObsolete>(groupsRepo, groupAccessesRepo, user, courseId, groupsIds);
		filterOptions.SlidesIds = new List<Guid> { slide.Id };

		if (slide is ExerciseSlide)
			return (await slideCheckingsRepo.GetManualCheckingQueue<ManualExerciseChecking>(filterOptions)).Count;
		if (slide is QuizSlide)
			return (await slideCheckingsRepo.GetManualCheckingQueue<ManualQuizChecking>(filterOptions)).Count;

		throw new ArgumentException("Slide should be quiz or exercise", nameof(slide));
	}
}