using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Database.Models;
using Database.Repos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Courses.Slides.Exercises.Blocks;
using Ulearn.Core.RunCheckerJobApi;
using Ulearn.Core.Telegram;
using Ulearn.Web.Api.Utils.Courses;
using Vostok.Logging.Abstractions;
using Web.Api.Configuration;

namespace Ulearn.Web.Api.Controllers.Runner
{
	[ApiController]
	[Produces("application/json")]
	public class RunnerGetSubmissionsController : ControllerBase
	{
		private readonly WebApiConfiguration configuration;
		private readonly IServiceScopeFactory serviceScopeFactory;

		public RunnerGetSubmissionsController(IOptions<WebApiConfiguration> options, IServiceScopeFactory serviceScopeFactory)
		{
			configuration = options.Value;
			this.serviceScopeFactory = serviceScopeFactory;
		}

		private static ILog Log => LogProvider.Get().ForContext(typeof(RunnerGetSubmissionsController));

		/// <summary>
		///     Взять на проверку решения задач
		/// </summary>
		[HttpPost("/runner/get-submissions")]
		public async Task<ActionResult<List<RunnerSubmission>>> GetSubmissions([FromQuery] string token, [FromQuery] string sandboxes, [FromQuery] string agent)
		{
			if (configuration.RunnerToken != token)
				return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("Invalid token"));

			var sandboxesImageNames = sandboxes.Split(',').ToList();

			var sw = Stopwatch.StartNew();
			while (true)
			{
				using (var scope = serviceScopeFactory.CreateScope())
				{
					var userSolutionsRepo = (IUserSolutionsRepo)scope.ServiceProvider.GetService(typeof(IUserSolutionsRepo))!;
					var submission = await userSolutionsRepo.GetUnhandledSubmission(agent, sandboxesImageNames);
					if (submission is not null || sw.Elapsed > TimeSpan.FromSeconds(10))
					{
						if (submission is not null)
							Log.Info($"Отдаю на проверку решение: [{submission.Id}], агент {agent}, только сначала соберу их");
						else
							return new List<RunnerSubmission>();

						var courseStorage = scope.ServiceProvider.GetService<ICourseStorage>();
						var courseManager = scope.ServiceProvider.GetService<IMasterCourseManager>();
						try
						{
							var builtSubmissions = new List<RunnerSubmission> { ToRunnerSubmission(submission, courseStorage, courseManager) };
							Log.Info($"Собрал решения: [{submission.Id}], отдаю их агенту {agent}");
							return builtSubmissions;
						}
						catch (Exception ex)
						{
							await userSolutionsRepo.SetExceptionStatusForSubmission(submission);
							Log.Error(ex);
							var errorsBot = scope.ServiceProvider.GetService<ErrorsBot>();
							var slide = courseStorage.GetCourse(submission.CourseId).GetSlideByIdNotSafe(submission.SlideId);
							await errorsBot.PostToChannelAsync($"Не смог собрать архив с решением для проверки.\nКурс «{submission.CourseId}», слайд «{slide.Title}» ({submission.SlideId})\n\nhttps://ulearn.me/Sandbox");
							continue;
						}
					}
				}

				await Task.Delay(TimeSpan.FromMilliseconds(50));
				await UnhandledSubmissionsWaiter.WaitAnyUnhandledSubmissions(TimeSpan.FromSeconds(8));
			}
		}

		private static RunnerSubmission ToRunnerSubmission(UserExerciseSubmission submission,
			ICourseStorage courseStorage, IMasterCourseManager courseManager)
		{
			Log.Info($"Собираю для отправки в RunCsJob решение {submission.Id}");
			var slide = courseStorage.FindCourse(submission.CourseId)?.FindSlideByIdNotSafe(submission.SlideId);

			if (slide is not ExerciseSlide exerciseSlide)
				return new FileRunnerSubmission
				{
					Id = submission.Id.ToString(),
					Code = "// no slide anymore",
					Input = "",
					NeedRun = true
				};

			var courseDictionary = courseManager.GetExtractedCourseDirectory(submission.CourseId).FullName;
			if (exerciseSlide is PolygonExerciseSlide)
				return ((PolygonExerciseBlock)exerciseSlide.Exercise).CreateSubmission(
					submission.Id.ToString(),
					submission.SolutionCode.Text,
					submission.Language,
					courseDictionary
				);

			return exerciseSlide.Exercise.CreateSubmission(
				submission.Id.ToString(),
				submission.SolutionCode.Text,
				courseDictionary
			);
		}
	}
}