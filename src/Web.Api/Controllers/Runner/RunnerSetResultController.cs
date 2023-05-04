using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ulearn.Common;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Metrics;
using Ulearn.Core.RunCheckerJobApi;
using Vostok.Logging.Abstractions;
using Web.Api.Configuration;

namespace Ulearn.Web.Api.Controllers.Runner;

public class RunnerSetResultController : BaseController
{
	private readonly WebApiConfiguration configuration;
	private readonly IGroupsRepo groupsRepo;
	private readonly MetricSender metricSender;
	private readonly List<IResultObserver> resultObservers;
	private readonly ISlideCheckingsRepo slideCheckingsRepo;
	private readonly IUserSolutionsRepo userSolutionsRepo;
	private readonly IVisitsRepo visitsRepo;

	public RunnerSetResultController(ICourseStorage courseStorage, UlearnDb db, IOptions<WebApiConfiguration> options,
		IUsersRepo usersRepo, IUserSolutionsRepo userSolutionsRepo, ISlideCheckingsRepo slideCheckingsRepo,
		IGroupsRepo groupsRepo, IVisitsRepo visitsRepo, MetricSender metricSender,
		XQueueResultObserver xQueueResultObserver, SandboxErrorsResultObserver sandboxErrorsResultObserver,
		AntiPlagiarismResultObserver antiPlagiarismResultObserver, StyleErrorsResultObserver styleErrorsResultObserver, LtiResultObserver ltiResultObserver)
		: base(courseStorage, db, usersRepo)
	{
		configuration = options.Value;
		resultObservers = new List<IResultObserver>
		{
			xQueueResultObserver,
			ltiResultObserver,
			sandboxErrorsResultObserver,
			antiPlagiarismResultObserver,
			styleErrorsResultObserver
		};
		this.userSolutionsRepo = userSolutionsRepo;
		this.slideCheckingsRepo = slideCheckingsRepo;
		this.groupsRepo = groupsRepo;
		this.visitsRepo = visitsRepo;
		this.metricSender = metricSender;
	}

	private new static ILog Log => LogProvider.Get().ForContext(typeof(RunnerSetResultController));

	/// <summary>
	///     Записать результат проверки решений задач
	/// </summary>
	[HttpPost("/runner/set-result")]
	public async Task<ActionResult> SetResults([FromQuery] string token, [FromQuery] string agent, [FromBody] List<RunningResults> results)
	{
		if (!ModelState.IsValid)
		{
			var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
			var errorStr = $"Не могу принять от RunCsJob результаты проверки решений, ошибки: {string.Join(", ", errors)}";
			Log.Error(errorStr);
			return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse(errorStr));
		}

		if (configuration.RunnerToken != token)
			return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("Invalid token"));
		Log.Info($"Получил от RunCsJob результаты проверки решений: [{string.Join(", ", results.Select(r => r.Id))}] от агента {agent}");

		foreach (var result in results)
			await FuncUtils.TrySeveralTimesAsync(() => userSolutionsRepo.SaveResult(result,
				submission => SendToReviewAndUpdateScore(submission, courseStorage, slideCheckingsRepo, groupsRepo, visitsRepo, metricSender)
			), 3).ConfigureAwait(false);

		var submissionsByIds = (await userSolutionsRepo
				.FindSubmissionsByIds(results
					.Select(result => int.TryParse(result.Id, out var parsed) ? parsed : -1)
					.Where(i => i != -1)
					.Distinct()
					.ToList())
			).ToDictionary(s => s.Id.ToString());

		foreach (var result in results)
		{
			if (!submissionsByIds.ContainsKey(result.Id))
				continue;
			await SendResultToObservers(submissionsByIds[result.Id], result);
		}

		return StatusCode((int)HttpStatusCode.Accepted);
	}

	public static async Task<bool> SendToReviewAndUpdateScore(UserExerciseSubmission submissionNoTracking,
		ICourseStorage courseStorage, ISlideCheckingsRepo slideCheckingsRepo, IGroupsRepo groupsRepo, IVisitsRepo visitsRepo, MetricSender metricSender)
	{
		var userId = submissionNoTracking.UserId;
		var courseId = submissionNoTracking.CourseId;
		var course = courseStorage.GetCourse(courseId);
		var exerciseSlide = course.FindSlideByIdNotSafe(submissionNoTracking.SlideId) as ExerciseSlide; // SlideId проверен в вызывающем методе 
		if (exerciseSlide is null)
			return false;
		var exerciseMetricId = GetExerciseMetricId(courseId, exerciseSlide);
		var automaticCheckingNoTracking = submissionNoTracking.AutomaticChecking;
		var isProhibitedUserToSendForReview = await slideCheckingsRepo.IsProhibitedToSendExerciseToManualChecking(courseId, exerciseSlide.Id, userId);
		var sendToReview = exerciseSlide.Scoring.RequireReview
							&& submissionNoTracking.AutomaticCheckingIsRightAnswer
							&& !isProhibitedUserToSendForReview
							&& await groupsRepo.IsManualCheckingEnabledForUserAsync(course, userId);
		if (sendToReview)
		{
			await slideCheckingsRepo.RemoveWaitingManualCheckings<ManualExerciseChecking>(courseId, exerciseSlide.Id, userId, false);
			await slideCheckingsRepo.AddManualExerciseChecking(courseId, exerciseSlide.Id, userId, submissionNoTracking.Id);
			await visitsRepo.MarkVisitsAsWithManualChecking(courseId, exerciseSlide.Id, userId);
			metricSender.SendCount($"exercise.{exerciseMetricId}.sent_to_review");
			metricSender.SendCount("exercise.sent_to_review");
		}

		await visitsRepo.UpdateScoreForVisit(courseId, exerciseSlide, userId);

		if (automaticCheckingNoTracking is null)
			return sendToReview;
		
		var verdictForMetric = automaticCheckingNoTracking.GetVerdict().Replace(" ", "");
		metricSender.SendCount($"exercise.{exerciseMetricId}.{verdictForMetric}");

		return sendToReview;
	}

	public static string GetExerciseMetricId(string courseId, ExerciseSlide exerciseSlide)
	{
		var slideTitleForMetric = exerciseSlide.LatinTitle.Replace(".", "_").ToLower(CultureInfo.InvariantCulture);
		if (slideTitleForMetric.Length > 25)
			slideTitleForMetric = slideTitleForMetric[..25];
		return $"{courseId.ToLower(CultureInfo.InvariantCulture)}.{exerciseSlide.Id.ToString("N")[(32 - 25)..]}.{slideTitleForMetric}";
	}

	private async Task SendResultToObservers(UserExerciseSubmission submission, RunningResults result)
	{
		foreach (var o in resultObservers)
			await o.ProcessResult(submission, result);
	}
}