using System;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Web.Api.Controllers.Slides;
using Ulearn.Web.Api.Models.Responses.ExerciseStatistics;

namespace Ulearn.Web.Api.Controllers
{
	[Route("/exercise-statistics")]
	public class ExerciseStatisticsController : BaseController
	{
		private readonly ICourseRolesRepo courseRolesRepo;
		private readonly ICoursesRepo coursesRepo;
		private readonly IGroupsRepo groupsRepo;
		private readonly SlideRenderer slideRenderer;
		private readonly IUserSolutionsRepo solutionsRepo;
		private readonly IUnitsRepo unitsRepo;
		private readonly IUserQuizzesRepo userQuizzesRepo;
		private readonly IUserSolutionsRepo userSolutionsRepo;
		private readonly IVisitsRepo visitsRepo;

		public ExerciseStatisticsController(ICourseStorage courseStorage, IUserSolutionsRepo userSolutionsRepo, UlearnDb db, IUsersRepo usersRepo,
			IUserSolutionsRepo solutionsRepo, IUserQuizzesRepo userQuizzesRepo, IVisitsRepo visitsRepo, IGroupsRepo groupsRepo,
			SlideRenderer slideRenderer, ICourseRolesRepo courseRolesRepo, ICoursesRepo coursesRepo, IUnitsRepo unitsRepo)
			: base(courseStorage, db, usersRepo)
		{
			this.coursesRepo = coursesRepo;
			this.courseRolesRepo = courseRolesRepo;
			this.userSolutionsRepo = userSolutionsRepo;
			this.solutionsRepo = solutionsRepo;
			this.userQuizzesRepo = userQuizzesRepo;
			this.visitsRepo = visitsRepo;
			this.groupsRepo = groupsRepo;
			this.slideRenderer = slideRenderer;
			this.unitsRepo = unitsRepo;
		}

		/// <summary>
		///     Статистика по выполнению каждого упражнения в курсе
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<CourseExercisesStatisticsResponse>> CourseStatistics([FromQuery] [BindRequired] string courseId,
			int count = 10000, DateTime? from = null, DateTime? to = null)
		{
			var course = courseStorage.FindCourse(courseId);
			if (course is null)
				return NotFound();

			from ??= DateTime.MinValue;
			to ??= DateTime.MaxValue;

			count = Math.Min(count, 10000);

			var isInstructor = await courseRolesRepo.HasUserAccessToCourse(UserId, course.Id, CourseRoleType.Instructor).ConfigureAwait(false);
			var visibleUnitsIds = await unitsRepo.GetVisibleUnitIds(course, UserId);
			var exerciseSlides = course.GetSlides(isInstructor, visibleUnitsIds).OfType<ExerciseSlide>().ToList();
		
			var submissions = await userSolutionsRepo.GetAllSubmissions(course.Id, false)
				.Where(s => s.Timestamp >= from && s.Timestamp <= to)
				.OrderByDescending(s => s.Timestamp)
				.Take(count)
				.Select(s => Tuple.Create(s.SlideId, s.AutomaticCheckingIsRightAnswer, s.Timestamp))
				.ToListAsync().ConfigureAwait(false);

			var getSlideMaxScoreFunc = await BuildGetSlideMaxScoreFunc(solutionsRepo, userQuizzesRepo, visitsRepo, groupsRepo, course, User.GetUserId());
			var getGitEditLinkFunc = await BuildGetGitEditLinkFunc(User.GetUserId(), course, courseRolesRepo, coursesRepo);

			const int daysLimit = 30;

			var result = new CourseExercisesStatisticsResponse
			{
				AnalyzedSubmissionsCount = submissions.Count,
				Exercises = exerciseSlides.Select(
					slide =>
					{
						/* Statistics for this exercise slide: */
						var exerciseSubmissions = submissions.Where(s => s.Item1 == slide.Id).ToList();
						return new OneExerciseStatistics
						{
							Exercise = slideRenderer.BuildShortSlideInfo(course.Id, slide, getSlideMaxScoreFunc, getGitEditLinkFunc, Url),
							SubmissionsCount = exerciseSubmissions.Count,
							AcceptedCount = exerciseSubmissions.Count(s => s.Item2),
							/* Select last 30 (`datesLimit`) dates */
							LastDates = exerciseSubmissions.GroupBy(s => s.Item3.Date).OrderByDescending(g => g.Key).Take(daysLimit).ToDictionary(
								/* Date: */
								g => g.Key,
								/* Statistics for this date: */
								g => new OneExerciseStatisticsForDate
								{
									SubmissionsCount = g.Count(),
									AcceptedCount = g.Count(s => s.Item2)
								}
							)
						};
					}).ToList()
			};

			return result;
		}
	}
}