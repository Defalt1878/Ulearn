using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.Users;
using Database.Utils;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Flashcards;
using Ulearn.Core.Courses.Units;
using Ulearn.Web.Api.Controllers.Slides;
using Ulearn.Web.Api.Models.Common;
using Ulearn.Web.Api.Models.Responses.Courses;
using Ulearn.Web.Api.Models.Responses.Groups;

namespace Ulearn.Web.Api.Controllers
{
	[Route("/courses")]
	public class CoursesController : BaseController
	{
		private readonly IAdditionalContentPublicationsRepo additionalContentPublicationsRepo;
		private readonly ICourseRolesRepo courseRolesRepo;
		private readonly ICoursesRepo coursesRepo;
		private readonly IGroupAccessesRepo groupAccessesRepo;
		private readonly IGroupMembersRepo groupMembersRepo;
		private readonly IGroupsRepo groupsRepo;
		private readonly SlideRenderer slideRenderer;
		private readonly IUserSolutionsRepo solutionsRepo;
		private readonly ITempCoursesRepo tempCoursesRepo;
		private readonly IUnitsRepo unitsRepo;
		private readonly IUserQuizzesRepo userQuizzesRepo;
		private readonly IVisitsRepo visitsRepo;

		public CoursesController(ICourseStorage courseStorage, UlearnDb db, ICoursesRepo coursesRepo,
			IUsersRepo usersRepo, ICourseRolesRepo courseRolesRepo, IUnitsRepo unitsRepo, IUserSolutionsRepo solutionsRepo,
			IUserQuizzesRepo userQuizzesRepo, IVisitsRepo visitsRepo, IGroupsRepo groupsRepo, IGroupMembersRepo groupMembersRepo,
			IGroupAccessesRepo groupAccessesRepo, SlideRenderer slideRenderer, IAdditionalContentPublicationsRepo additionalContentPublicationsRepo,
			ITempCoursesRepo tempCoursesRepo)
			: base(courseStorage, db, usersRepo)
		{
			this.coursesRepo = coursesRepo;
			this.courseRolesRepo = courseRolesRepo;
			this.unitsRepo = unitsRepo;
			this.solutionsRepo = solutionsRepo;
			this.userQuizzesRepo = userQuizzesRepo;
			this.visitsRepo = visitsRepo;
			this.groupsRepo = groupsRepo;
			this.groupMembersRepo = groupMembersRepo;
			this.groupAccessesRepo = groupAccessesRepo;
			this.slideRenderer = slideRenderer;
			this.additionalContentPublicationsRepo = additionalContentPublicationsRepo;
			this.tempCoursesRepo = tempCoursesRepo;
		}

		/// <summary>
		///     Список курсов
		/// </summary>
		/// <param name="role">
		///     Роль указывается, если нужно получить только те курсы, в которых пользователь имеет роль эту или
		///     выше
		/// </param>
		[HttpGet]
		public async Task<ActionResult<CoursesListResponse>> CoursesList([FromQuery] CourseRoleType? role = null)
		{
			if (role.HasValue && !IsAuthenticated)
				return Unauthorized();

			if (role == CourseRoleType.Student)
				return NotFound(new ErrorResponse("Role can not be student. Specify tester, instructor or courseAdmin"));

			var courses = courseStorage.GetCourses();

			var isSystemAdministrator = await IsSystemAdministratorAsync().ConfigureAwait(false);

			// Фильтрация по роли. У администратора высшая роль.
			if (role.HasValue && !isSystemAdministrator)
			{
				var courseIdsAsRole = await courseRolesRepo.GetCoursesWhereUserIsInRole(UserId, role.Value).ConfigureAwait(false);
				courses = courses.Where(c => courseIdsAsRole.Contains(c.Id, StringComparer.InvariantCultureIgnoreCase));
			}

			// Неопубликованные курсы не покажем тем, кто не имеет роли в них.
			if (!isSystemAdministrator)
			{
				var visibleCourses = await unitsRepo.GetVisibleCourses();
				var coursesInWhichUserHasAnyRole = await courseRolesRepo.GetCoursesWhereUserIsInRole(UserId, CourseRoleType.Tester).ConfigureAwait(false);
				var userGroups = await groupMembersRepo.GetUserGroupsAsync(User.GetUserId());
				var userGroupsIds = userGroups.Select(g => g.Id).ToHashSet();
				HashSet<string> publications = null;
				if (userGroups.Count > 0)
					publications = (await additionalContentPublicationsRepo.GetAdditionalContentPublications(userGroupsIds))
						.DistinctBy(p => p.CourseId)
						.Select(p => p.CourseId)
						.ToHashSet();

				courses = courses
					.Where(c => visibleCourses.Contains(c.Id)
								|| coursesInWhichUserHasAnyRole.Contains(c.Id, StringComparer.OrdinalIgnoreCase)
								|| (publications is not null && publications.Contains(c.Id)));
			}

			// Администратор видит все курсы. Покажем сверху те, в которых он преподаватель.
			if (isSystemAdministrator)
			{
				var instructorCourseIds = await courseRolesRepo.GetCoursesWhereUserIsInStrictRole(UserId, CourseRoleType.Instructor).ConfigureAwait(false);
				courses = courses.OrderBy(c => !instructorCourseIds.Contains(c.Id, StringComparer.InvariantCultureIgnoreCase)).ThenBy(c => c.Title);
			}
			else
			{
				courses = courses.OrderBy(c => c.Title);
			}

			var allTempCourses = (await tempCoursesRepo.GetAllTempCourses()) // Все, потому что старые могут быть еще в памяти.
				.ToDictionary(t => t.CourseId, t => t, StringComparer.InvariantCultureIgnoreCase);
			var coursesList = courses.ToList();
			var coursesLastVisits = await visitsRepo.GetLastVisitsForCourses(coursesList.Select(c => c.Id).ToHashSet(), UserId);
			return new CoursesListResponse
			{
				Courses = coursesList
					.Select(c => new ShortCourseInfo
						{
							Id = c.Id,
							Title = allTempCourses.TryGetValue(c.Id, out var tempCourse) ? tempCourse.GetVisibleName(c.Title) : c.Title,
							ApiUrl = Url.Action("CourseInfo", "Courses", new { courseId = c.Id }),
							IsTempCourse = allTempCourses.ContainsKey(c.Id),
							Timestamp = coursesLastVisits.TryGetValue(c.Id, out var date) ? date : null
						}
					).ToList()
			};
		}

		/// <summary>
		///     Информация о курсе
		/// </summary>
		/// <param name="groupId">If null, returns data for the current user, otherwise for a group</param>
		[HttpGet("{courseId}")]
		public async Task<ActionResult<CourseInfo>> CourseInfo([FromRoute] string courseId, [FromQuery] [CanBeNull] int? groupId = null)
		{
			if (!courseStorage.HasCourse(courseId))
				return NotFound(new ErrorResponse("Course not found"));

			var course = courseStorage.FindCourse(courseId);
			List<UnitInfo> units;
			var userId = UserId;
			var visibleUnitsIds = await unitsRepo.GetVisibleUnitIds(course, userId);
			var visibleUnits = course.GetUnits(visibleUnitsIds);
			var isInstructor = await courseRolesRepo.HasUserAccessToCourse(userId, course.Id, CourseRoleType.Instructor).ConfigureAwait(false);
			var isTester = isInstructor || await courseRolesRepo.HasUserAccessToCourse(userId, course.Id, CourseRoleType.Tester).ConfigureAwait(false);

			if (groupId is null)
			{
				if (!isInstructor && visibleUnits.Count == 0)
					return NotFound(new ErrorResponse("Course not found"));

				var unitAppearances = !isInstructor
					? new Dictionary<Guid, UnitAppearance>()
					: (await unitsRepo.GetUnitAppearances(course)).ToDictionary(a => a.UnitId, a => a);
				var publishedUnitIds = new HashSet<Guid>(!isInstructor ? visibleUnitsIds : await unitsRepo.GetPublishedUnitIds(course));
				var getSlideMaxScoreFunc = await BuildGetSlideMaxScoreFunc(solutionsRepo, userQuizzesRepo, visitsRepo, groupsRepo, course, userId);
				var getGitEditLinkFunc = await BuildGetGitEditLinkFunc(userId, course, courseRolesRepo, coursesRepo);
				units = visibleUnits.Select(unit =>
						BuildUnitInfo(
							course.Id,
							unit,
							!publishedUnitIds.Contains(unit.Id),
							publishedUnitIds.Contains(unit.Id) ? null : unitAppearances.GetOrDefault(unit.Id)?.PublishTime,
							isInstructor,
							getSlideMaxScoreFunc,
							getGitEditLinkFunc))
					.ToList();
			}
			else
			{
				var group = await groupsRepo.FindGroupByIdAsync(groupId.Value).ConfigureAwait(false) as SingleGroup;
				if (group is null)
					return NotFound(new ErrorResponse("Group not found"));

				async Task<bool> IsUserMemberOfGroup()
				{
					return await groupMembersRepo.IsUserMemberOfGroup(groupId.Value, userId).ConfigureAwait(false);
				}

				async Task<bool> IsGroupVisibleForUserAsync()
				{
					return await groupAccessesRepo.IsGroupVisibleForUserAsync(groupId.Value, userId).ConfigureAwait(false);
				}

				var isGroupAvailableForUser = await IsUserMemberOfGroup() || await IsGroupVisibleForUserAsync();
				if (!isGroupAvailableForUser)
					return NotFound(new ErrorResponse("Group not found"));

				if (visibleUnits.Count == 0)
					return NotFound(new ErrorResponse("Course not found"));

				var getSlideMaxScoreFunc = BuildGetSlideMaxScoreFunc(course, group);
				var getGitEditLinkFunc = await BuildGetGitEditLinkFunc(userId, course, courseRolesRepo, coursesRepo);
				units = visibleUnits.Select(unit => BuildUnitInfo(course.Id, unit, false, null, false, getSlideMaxScoreFunc, getGitEditLinkFunc)).ToList();
			}

			var (publishedUnits, publishedSlides) = await AdditionalContentPublicationUtils.GetPublications(groupMembersRepo, additionalContentPublicationsRepo, course.Id, userId);

			units = units
				.Select(u =>
				{
					if (publishedUnits.TryGetValue(u.Id, out var unit))
					{
						u.AdditionalContentInfo.PublicationDate = unit.Date.ToString("yyyy-MM-ddTHH:mm:ss");
						if (!isTester && unit.Date > DateTime.Now)
							u.Slides = new List<ShortSlideInfo>();
					}
					else if (!isTester && u.AdditionalContentInfo.IsAdditionalContent)
					{
						return null;
					}

					var slides = u.Slides
						.Select(s =>
						{
							if (publishedSlides.TryGetValue(s.Id, out var slide))
								s.AdditionalContentInfo.PublicationDate = slide.Date.ToString("yyyy-MM-ddTHH:mm:ss");
							else if (!isTester && s.AdditionalContentInfo.IsAdditionalContent)
								return null;
							return s;
						})
						.Where(s => s is not null)
						.ToList();

					return new UnitInfo
					{
						Id = u.Id,
						Title = u.Title,
						AdditionalScores = new List<UnitScoringGroupInfo>(u.AdditionalScores),
						PublicationDate = u.PublicationDate,
						IsNotPublished = u.IsNotPublished,
						Slides = slides,
						AdditionalContentInfo = u.AdditionalContentInfo
					};
				})
				.Where(u => u is not null)
				.ToList();

			if (units.Count == 0)
				return NotFound(new ErrorResponse("Course not found"));

			var containsFlashcards = visibleUnits.Any(x => x.GetSlides(true).OfType<FlashcardSlide>().Any());
			var scoringSettings = GetScoringSettings(course);
			var tempCourseError = (await tempCoursesRepo.GetCourseError(courseId))?.Error;
			var tempCourse = await tempCoursesRepo.Find(courseId);
			return new CourseInfo
			{
				Id = course.Id,
				Title = tempCourse is not null ? tempCourse.GetVisibleName(course.Title) : course.Title,
				Description = course.Settings.Description,
				Scoring = scoringSettings,
				NextUnitPublishTime = await unitsRepo.GetNextUnitPublishTime(course.Id),
				Units = units,
				ContainsFlashcards = containsFlashcards,
				IsTempCourse = tempCourse is not null,
				TempCourseError = tempCourseError
			};
		}

		private ScoringSettingsModel GetScoringSettings(Course course)
		{
			var groups = course.Settings.Scoring.Groups.Values
				.Concat(new[] { course.Settings.Scoring.VisitsGroup })
				.Where(sg => sg is not null)
				.Select(sg => new ScoringGroupModel
				{
					Id = sg.Id,
					Name = sg.Name,
					Abbr = sg.Abbreviation.NullIfEmptyOrWhitespace(),
					Description = sg.Description.NullIfEmptyOrWhitespace(),
					Weight = sg.Weight
				})
				.ToList();
			return new ScoringSettingsModel { Groups = groups };
		}

		private UnitInfo BuildUnitInfo(string courseId, Unit unit, bool isNotPublished, DateTime? publicationDate, bool isInstructor, Func<Slide, int> getSlideMaxScoreFunc, Func<Slide, string> getGitEditLinkFunc)
		{
			var slides = unit
				.GetSlides(isInstructor)
				.Select(slide => slideRenderer.BuildShortSlideInfo(courseId, slide, getSlideMaxScoreFunc, getGitEditLinkFunc, Url));

			if (isInstructor && unit.InstructorNote is not null)
				slides = slides.Concat(new List<ShortSlideInfo> { slideRenderer.BuildShortSlideInfo(courseId, unit.InstructorNote, getSlideMaxScoreFunc, getGitEditLinkFunc, Url) });
			return BuildUnitInfo(unit, isNotPublished, publicationDate, slides);
		}

		private static UnitInfo BuildUnitInfo(Unit unit, bool isNotPublished, DateTime? publicationDate, IEnumerable<ShortSlideInfo> slides)
		{
			return new UnitInfo
			{
				Id = unit.Id,
				Title = unit.Title,
				IsNotPublished = isNotPublished,
				PublicationDate = publicationDate,
				Slides = slides.ToList(),
				AdditionalScores = GetAdditionalScores(unit),
				AdditionalContentInfo = new AdditionalContent
				{
					IsAdditionalContent = unit.Settings.IsExtraContent,
					PublicationDate = null
				}
			};
		}

		private static List<UnitScoringGroupInfo> GetAdditionalScores(Unit unit)
		{
			return unit.Settings.Scoring.Groups.Values.Where(g => g.CanBeSetByInstructor).Select(g => new UnitScoringGroupInfo(g)).ToList();
		}
	}
}