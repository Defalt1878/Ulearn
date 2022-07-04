using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Database;
using Database.DataContexts;
using Database.Extensions;
using Database.Models;
using Elmah;
using Vostok.Logging.Abstractions;
using LtiLibrary.Owin.Security.Lti;
using uLearn.Web.Extensions;
using uLearn.Web.FilterAttributes;
using uLearn.Web.LTI;
using Ulearn.Common.Extensions;
using Ulearn.Core;
using Ulearn.Core.Configuration;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Courses.Slides.Exercises.Blocks;
using Ulearn.Core.Courses.Slides.Quizzes;
using Ulearn.Core.Courses.Units;
using Ulearn.Core.Markdown;
using uLearn.Web.Core.Models;

namespace uLearn.Web.Controllers;

[ULearnAuthorize]
public class CourseController : BaseController
{
	private static ILog log => LogProvider.Get().ForContext(typeof(CourseController));

	private readonly ULearnDb db = new ULearnDb();
	private readonly ICourseStorage courseStorage = WebCourseManager.CourseStorageInstance;

	private readonly string baseUrlApi;
	private readonly string baseUrlWeb;

	private readonly UserSolutionsRepo solutionsRepo;
	private readonly UnitsRepo unitsRepo;
	private readonly VisitsRepo visitsRepo;
	private readonly LtiRequestsRepo ltiRequestsRepo;
	private readonly SlideCheckingsRepo slideCheckingsRepo;
	private readonly GroupsRepo groupsRepo;
	private readonly UserQuizzesRepo userQuizzesRepo;
	private readonly CoursesRepo coursesRepo;
	private readonly TempCoursesRepo tempCoursesRepo;
	private readonly UserRolesRepo userRolesRepo;

	public CourseController()
	{
		var configuration = ApplicationConfiguration.Read<UlearnConfiguration>();
		baseUrlWeb = configuration.BaseUrl;
		baseUrlApi = configuration.BaseUrlApi;

		slideCheckingsRepo = new SlideCheckingsRepo(db);
		visitsRepo = new VisitsRepo(db);
		unitsRepo = new UnitsRepo(db);
		solutionsRepo = new UserSolutionsRepo(db);
		ltiRequestsRepo = new LtiRequestsRepo(db);
		groupsRepo = new GroupsRepo(db, courseStorage);
		userQuizzesRepo = new UserQuizzesRepo(db);
		coursesRepo = new CoursesRepo(db);
		tempCoursesRepo = new TempCoursesRepo(db);
		userRolesRepo = new UserRolesRepo(db);
	}

	[AllowAnonymous]
	public async Task<ActionResult> SlideById(string courseId, string slideId = "", int? checkQueueItemId = null, int? version = null, int autoplay = 0)
	{
		if (slideId.Contains("_"))
			slideId = slideId.Substring(slideId.LastIndexOf('_') + 1);

		// По крайней мере одно из мест использования groupsIds: переход на следующее ревью после выполнения предыдущего.
		var groupsIds = Request.GetMultipleValuesFromQueryString("group");

		if (!Guid.TryParse(slideId, out var slideGuid))
			return HttpNotFound();

		if (string.IsNullOrWhiteSpace(courseId))
		{
			return RedirectToAction("Index", "Home");
		}

		var course = courseStorage.FindCourse(courseId);
		if (course == null)
			return HttpNotFound();

		var visibleUnitIds = unitsRepo.GetVisibleUnitIds(course, User).ToList();
		var visibleUnits = course.GetUnits(visibleUnitIds);
		var isGuest = !User.Identity.IsAuthenticated;
		var isInstructor = !isGuest && User.HasAccessFor(course.Id, CourseRole.Instructor);

		var slide = slideGuid == Guid.Empty
			? GetInitialSlideForStartup(courseId, visibleUnits, isInstructor)
			: course.FindSlideById(slideGuid, isInstructor, visibleUnitIds);

		if (slide == null)
		{
			var instructorNote = course.FindInstructorNoteByIdNotSafe(slideGuid);
			if (instructorNote != null && isInstructor)
				slide = instructorNote;
		}

		if (slide == null)
			return HttpNotFound();
			
		if ((slide.IsExtraContent || slide.Unit.Settings.IsExtraContent) && (!isInstructor || isGuest || !User.HasAccessFor(course.Id, CourseRole.Tester)))
		{
			if (isGuest)
				return HttpNotFound();

			var userGroups = groupsRepo.GetUserGroups(course.Id, User.Identity.GetUserId())
				.Select(g => g.Id)
				.ToList();

			if (userGroups.Count == 0)
				return HttpNotFound();

			var unitPublications = db.AdditionalContentPublications
				.Where(ac => ac.CourseId == courseId && ac.UnitId == slide.Unit.Id && userGroups.Contains(ac.GroupId))
				.ToList();

			if (!unitPublications.Any())
				return HttpNotFound();

			if (slide.Unit.Settings.IsExtraContent && !unitPublications.Any(p => p.SlideId == null && DateTime.Now >= p.Date))
				return HttpNotFound();

			if (slide.IsExtraContent && !unitPublications.Any(p => p.SlideId == slide.Id && DateTime.Now >= p.Date))
				return HttpNotFound();
		}

		AbstractManualSlideChecking queueItem = null;
		var isManualCheckingReadonly = false;
		if (User.HasAccessFor(courseId, CourseRole.Instructor) && checkQueueItemId != null)
		{
			if (slide is QuizSlide)
				queueItem = slideCheckingsRepo.FindManualCheckingById<ManualQuizChecking>(checkQueueItemId.Value);
			if (slide is ExerciseSlide)
				queueItem = slideCheckingsRepo.FindManualCheckingById<ManualExerciseChecking>(checkQueueItemId.Value);

			if (queueItem == null)
			{
				/* It's possible when checking has not been fully checked, lock has been released, but after it user re-send his solution and we removed old waiting checking */
				return RedirectToAction("CheckingQueue", "Admin", new
				{
					courseId = courseId,
					message = "checking_removed"
				});
			}
		}

		var model = isGuest ?
			CreateGuestCoursePageModel(course, slide, autoplay > 0) :
			await CreateCoursePageModel(course, slide, queueItem, version, groupsIds, autoplay > 0, isManualCheckingReadonly);

		if (!string.IsNullOrEmpty(Request.QueryString["error"]))
			model.Error = Request.QueryString["error"];

		if (!visibleUnits.Contains(model.Slide.Unit))
			return HttpNotFound("Slide is hidden " + slideGuid);
		return View("Slide", model);
	}

	[AllowAnonymous]
	public async Task<ActionResult> Slide(string courseId)
	{
		var course = courseStorage.FindCourse(courseId);
		if (course == null)
			return HttpNotFound();
		var visibleUnitIds = unitsRepo.GetVisibleUnitIds(course, User);
		var visibleUnits = course.GetUnits(visibleUnitIds);
		var isInstructor = User.HasAccessFor(course.Id, CourseRole.Instructor);
		var slide = GetInitialSlideForStartup(courseId, visibleUnits, isInstructor);
		if (slide == null)
			return HttpNotFound();
		return RedirectToRoute("Course.SlideById", new { courseId, slideId = slide.Url });
	}

	[AllowAnonymous]
	public async Task<ActionResult> LtiSlide(string courseId, Guid slideId)
	{
		if (string.IsNullOrWhiteSpace(courseId))
			return RedirectToAction("Index", "Home");

		var course = courseStorage.GetCourse(courseId);
		var visibleUnitIds = unitsRepo.GetVisibleUnitIds(course);
		var slide = course.GetSlideById(slideId, false, visibleUnitIds);

		string userId;
		var owinRequest = Request.GetOwinContext().Request;
		if (await owinRequest.IsAuthenticatedLtiRequestAsync())
		{
			var ltiRequest = await owinRequest.ParseLtiRequestAsync();
			log.Info($"Нашёл LTI request в запросе: {ltiRequest.JsonSerialize()}");
			userId = Request.GetOwinContext().Authentication.AuthenticationResponseGrant.Identity.GetUserId();
			await ltiRequestsRepo.Update(courseId, userId, slide.Id, ltiRequest.JsonSerialize());

			/* Substitute http(s) scheme with real scheme from header */
			var uriBuilder = new UriBuilder(ltiRequest.Url)
			{
				Scheme = owinRequest.GetRealRequestScheme(),
				Port = owinRequest.GetRealRequestPort()
			};
			return Redirect(uriBuilder.Uri.AbsoluteUri);
		}

		/* For now user should be authenticated */
		if (!User.Identity.IsAuthenticated)
			return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

		userId = User.Identity.GetUserId();
		var visit = await VisitSlide(courseId, slide.Id, userId);

		/* Try to send score via LTI immediately after slide visiting */
		try
		{
			if (visit.IsPassed)
				LtiUtils.SubmitScore(courseId, slide, userId, visit);
		}
		catch (Exception e)
		{
			ErrorLog.GetDefault(System.Web.HttpContext.Current).Log(new Error(e));
		}

		// Exercise обрабатывается реактом

		var quizSlide = slide as QuizSlide;
		if (quizSlide != null)
		{
			var model = new LtiQuizModel
			{
				CourseId = courseId,
				Slide = quizSlide,
				UserId = userId
			};
			return View("LtiQuizSlide", model);
		}

		return View();
	}

	private Slide GetInitialSlideForStartup(string courseId, List<Unit> orderedVisibleUnits, bool isInstructor)
	{
		var userId = User.Identity.GetUserId();
		var lastVisit = visitsRepo.FindLastVisit(courseId, userId);
		var orderedVisibleSlides = orderedVisibleUnits
			.Where(u => isInstructor || !u.Settings.IsExtraContent)
			.SelectMany(u => u.GetSlides(isInstructor))
			.Where(s => isInstructor || !s.IsExtraContent)
			.ToList();
		if (lastVisit != null)
		{
			var lastVisitedSlide = orderedVisibleSlides.FirstOrDefault(s => s.Id == lastVisit.SlideId);
			if (lastVisitedSlide != null)
				return lastVisitedSlide;
			if (isInstructor)
			{
				var instructorNoteSlide = orderedVisibleUnits.FirstOrDefault(u => u.Id == lastVisit.SlideId)?.InstructorNote;
				if (instructorNoteSlide != null)
					return instructorNoteSlide;
			}
		}

		var unorderedVisitedIds = visitsRepo.GetIdOfVisitedSlides(courseId, userId);
		var lastVisitedVisibleSlide = orderedVisibleSlides.LastOrDefault(slide => unorderedVisitedIds.Contains(slide.Id));
		if (lastVisitedVisibleSlide != null)
			return lastVisitedVisibleSlide;
		return orderedVisibleSlides.Any() ? orderedVisibleSlides.First() : null;
	}

	private CoursePageModel CreateGuestCoursePageModel(Course course, Slide slide, bool autoplay)
	{
		return new CoursePageModel
		{
			CourseId = course.Id,
			CourseTitle = course.Title,
			Slide = slide,
			BlockRenderContext = new BlockRenderContext(
				course,
				slide,
				baseUrlApi,
				baseUrlWeb,
				slide.Blocks.Select(block => block is AbstractExerciseBlock ? new ExerciseBlockData(course.Id, (ExerciseSlide)slide, false) { Url = Url } : (dynamic)null).ToArray(),
				isGuest: true,
				autoplay: autoplay),
			IsGuest = true,
		};
	}

	private async Task<CoursePageModel> CreateCoursePageModel(
		Course course, Slide slide,
		AbstractManualSlideChecking manualChecking, int? exerciseSubmissionId = null,
		List<string> groupsIds = null,
		bool autoplay = false,
		bool isManualCheckingReadonly = false)
	{
		var userId = User.Identity.GetUserId();

		if (manualChecking != null)
			userId = manualChecking.UserId;

		var defaultProhibitFurtherReview = groupsRepo.GetDefaultProhibitFutherReviewForUser(course.Id, userId, User);
		var manualCheckingsLeftInQueue = manualChecking != null ? ControllerUtils.GetManualCheckingsCountInQueue(slideCheckingsRepo, groupsRepo, User, course.Id, slide, groupsIds) : 0;

		var (notArchivedGroupNames, archivedGroupNames) = GetGroupNames(course, manualChecking);

		var model = new CoursePageModel
		{
			UserId = userId,
			CourseId = course.Id,
			CourseTitle = course.Title,
			Slide = slide,
			BlockRenderContext = CreateRenderContext(
				course, slide, manualChecking, exerciseSubmissionId, groupsIds,
				autoplay: autoplay,
				isManualCheckingReadonly: isManualCheckingReadonly,
				defaultProhibitFurtherReview: defaultProhibitFurtherReview, manualCheckingsLeftInQueue: manualCheckingsLeftInQueue),
			ManualChecking = manualChecking,
			ContextManualCheckingUserGroups = notArchivedGroupNames,
			ContextManualCheckingUserArchivedGroups = archivedGroupNames,
			IsGuest = false,
		};
		return model;
	}

	private (string, string) GetGroupNames(Course course, AbstractManualSlideChecking manualChecking)
	{
		var notArchivedGroupNames = "";
		var archivedGroupNames = "";
		if (manualChecking != null)
		{
			var userGroups = groupsRepo.GetUsersGroups(new List<string> { course.Id }, new List<string> { manualChecking.UserId }, User,
				actual: true, archived: true, 100);
			if (userGroups.ContainsKey(manualChecking.UserId))
			{
				notArchivedGroupNames = string.Join(", ", groupsRepo.GetUserGroupsNames(userGroups[manualChecking.UserId].Where(g => !g.IsArchived)));
				archivedGroupNames = string.Join(", ", groupsRepo.GetUserGroupsNames(userGroups[manualChecking.UserId].Where(g => g.IsArchived)));
			}
		}

		return (notArchivedGroupNames, archivedGroupNames);
	}

	// returns null if user can't edit git
	private string GetGitEditLink(Course course, string slideFilePathRelativeToCourse)
	{
		var courseRole = User.GetCourseRole(course.Id);
		var canEditGit = courseRole != null && courseRole <= CourseRole.CourseAdmin;
		if (!canEditGit)
			return null;
		var publishedCourseVersion = coursesRepo.GetPublishedCourseVersion(course.Id);
		if (publishedCourseVersion?.RepoUrl == null)
			return null;
		if (publishedCourseVersion.PathToCourseXml == null)
			return null;
		var branch = coursesRepo.GetCourseRepoSettings(course.Id)?.Branch ?? "master";
		return GitUtils.GetSlideEditLink(publishedCourseVersion.RepoUrl, branch, publishedCourseVersion.PathToCourseXml, slideFilePathRelativeToCourse);
	}

	private int GetMaxSlideScoreForUser(Course course, Slide slide, string userId)
	{
		var isSlideSolved = ControllerUtils.IsSlideSolved(solutionsRepo, userQuizzesRepo, course, userId, slide.Id);
		var hasManualChecking = visitsRepo.HasManualChecking(course.Id, userId, slide.Id);
		var enabledManualCheckingForUser = groupsRepo.IsManualCheckingEnabledForUser(course, userId);
		var maxSlideScore = ControllerUtils.GetMaxScoreForUsersSlide(slide, isSlideSolved, hasManualChecking, enabledManualCheckingForUser);
		return maxSlideScore;
	}

	private BlockRenderContext CreateRenderContext(Course course, Slide slide,
		AbstractManualSlideChecking manualChecking = null,
		int? exerciseSubmissionId = null, List<string> groupsIds = null, bool isLti = false,
		bool autoplay = false, bool isManualCheckingReadonly = false, bool defaultProhibitFurtherReview = true,
		int manualCheckingsLeftInQueue = 0)
	{
		/* ExerciseController will fill blockDatas later */
		var blockData = slide.Blocks.Select(b => (dynamic)null).ToArray();
		return new BlockRenderContext(
			course,
			slide,
			baseUrlApi,
			baseUrlWeb,
			blockData,
			isGuest: false,
			revealHidden: User.HasAccessFor(course.Id, CourseRole.Instructor),
			manualChecking: manualChecking,
			manualCheckingsLeftInQueue: manualCheckingsLeftInQueue,
			canUserFillQuiz: false,
			groupsIds: groupsIds,
			isLti: isLti,
			autoplay: autoplay,
			isManualCheckingReadonly: isManualCheckingReadonly,
			defaultProhibitFurtherReview: defaultProhibitFurtherReview
		)
		{
			VersionId = exerciseSubmissionId
		};
	}

	public async Task<Visit> VisitSlide(string courseId, Guid slideId, string userId)
	{
		if (string.IsNullOrEmpty(userId))
			return null;
		await visitsRepo.AddVisit(courseId, slideId, userId, GetRealClientIp());
		return visitsRepo.FindVisit(courseId, slideId, userId);
	}

	[ULearnAuthorize(MinAccessLevel = CourseRole.Instructor)]
	public ActionResult InstructorNote(string courseId, Guid unitId)
	{
		var course = courseStorage.GetCourse(courseId);
		var slide = course.GetUnitByIdNotSafe(unitId).InstructorNote;
		if (slide == null)
			return HttpNotFound("No instructor note for this unit");
		var gitEditUrl = GetGitEditLink(course, slide.SlideFilePathRelativeToCourse);
		return View(new InstructorNoteModel(slide, gitEditUrl, new MarkdownRenderContext(baseUrlApi, baseUrlWeb, courseId, slide.Unit.UnitDirectoryRelativeToCourse)));
	}

	[ULearnAuthorize(MinAccessLevel = CourseRole.Tester)]
	public async Task<ActionResult> ForgetAll(string courseId, Guid slideId)
	{
		var slide = courseStorage.GetCourse(courseId).GetSlideByIdNotSafe(slideId);
		var userId = User.Identity.GetUserId();
		db.SolutionLikes.RemoveRange(db.SolutionLikes.Where(q => q.UserId == userId && q.Submission.SlideId == slideId));

		db.UserExerciseSubmissions.RemoveSlideAction(courseId, slideId, userId);
		db.UserQuizSubmissions.RemoveSlideAction(courseId, slideId, userId);
		db.Visits.RemoveSlideAction(courseId, slideId, userId);
		await slideCheckingsRepo.RemoveAttempts(courseId, slideId, userId, false);

		db.Hints.RemoveSlideAction(courseId, slideId, userId);
		await db.SaveChangesAsync();

		return RedirectToAction("SlideById", new { courseId, slideId = slide.Id });
	}

	public async Task<ActionResult> Courses(string courseId = null, string courseTitle = null)
	{
		var isSystemAdministrator = User.IsSystemAdministrator();
		var userId = User.Identity.GetUserId();
		var courses = courseStorage.GetCourses();

		// Неопубликованные курсы не покажем тем, кто не имеет роли в них.
		if (!isSystemAdministrator)
		{
			var visibleCourses = unitsRepo.GetVisibleCourses();
			var coursesInWhichUserHasAnyRole = userRolesRepo.GetCoursesWhereUserIsInRole(userId, CourseRole.Tester);
			var coursesInWhichUserHasVisit = db.LastVisits
				.Where(v => visibleCourses.Contains(v.CourseId) && v.UserId == userId)
				.Select(v => v.CourseId)
				.Distinct()
				.Select(i => db.LastVisits
					.Where(v => v.CourseId == i && v.UserId == userId)
					.OrderByDescending(v => v.Timestamp)
					.FirstOrDefault())
				.ToDictionary(v => v!.CourseId, v => v.Timestamp != null, StringComparer.OrdinalIgnoreCase);
			var coursesWhereIAmStudent = groupsRepo.GetUserGroups(userId)
				.Select(g => g.CourseId)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Where(c => visibleCourses.Contains(c)).ToList();
			courses = courses.Where(c => coursesInWhichUserHasVisit[c.Id]
										|| coursesInWhichUserHasAnyRole.Contains(c.Id, StringComparer.OrdinalIgnoreCase)
										|| coursesWhereIAmStudent.Contains(c.Id, StringComparer.OrdinalIgnoreCase));
		}

		var incorrectChars = new string(CourseManager.InvalidForCourseIdCharacters.OrderBy(c => c).Where(c => 32 <= c).ToArray());
		if (isSystemAdministrator)
			courses = courses.OrderBy(course => course.Id, StringComparer.InvariantCultureIgnoreCase);
		else
			courses = courses.OrderBy(course => course.Title, StringComparer.InvariantCultureIgnoreCase);

		var allTempCourses = tempCoursesRepo.GetAllTempCourses()
			.ToDictionary(t => t.CourseId, t => t, StringComparer.InvariantCultureIgnoreCase);
		var model = new CourseListViewModel
		{
			Courses = courses
				.Select(course => new CourseViewModel
				{
					Id = course.Id,
					Title = course.Title,
					TempCourse = allTempCourses.GetOrDefault(course.Id)
				})
				.ToList(),
			LastTryCourseId = courseId,
			LastTryCourseTitle = courseTitle,
			InvalidCharacters = incorrectChars
		};
		return View(model);
	}
}