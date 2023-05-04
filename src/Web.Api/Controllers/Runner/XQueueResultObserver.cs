using System;
using System.Threading.Tasks;
using Database.Models;
using Database.Repos;
using Ulearn.Common;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.RunCheckerJobApi;
using Vostok.Logging.Abstractions;
using XQueue;
using XQueue.Models;

namespace Ulearn.Web.Api.Controllers.Runner
{
	public class XQueueResultObserver : IResultObserver
	{
		private readonly ICourseStorage courseStorage;
		private readonly IXQueueRepo xQueueRepo;

		public XQueueResultObserver(ICourseStorage courseStorage, IXQueueRepo xQueueRepo)
		{
			this.courseStorage = courseStorage;
			this.xQueueRepo = xQueueRepo;
		}

		private static ILog Log => LogProvider.Get().ForContext(typeof(XQueueResultObserver));

		public async Task ProcessResult(UserExerciseSubmission submission, RunningResults result)
		{
			var xQueueSubmission = await xQueueRepo.FindXQueueSubmission(submission);
			if (xQueueSubmission is null)
				return;

			var watcher = xQueueSubmission.Watcher;
			var client = new XQueueClient(watcher.BaseUrl, watcher.UserName, watcher.Password);
			await client.Login();
			if (await SendSubmissionResultsToQueue(client, xQueueSubmission))
				await xQueueRepo.MarkXQueueSubmissionThatResultIsSent(xQueueSubmission);
		}

		private async Task<bool> SendSubmissionResultsToQueue(XQueueClient client, XQueueExerciseSubmission submission)
		{
			return await FuncUtils.TrySeveralTimesAsync(
				() => TrySendSubmissionResultsToQueue(client, submission),
				5,
				() => Task.Delay(TimeSpan.FromMilliseconds(1)));
		}

		private async Task<bool> TrySendSubmissionResultsToQueue(XQueueClient client, XQueueExerciseSubmission submission)
		{
			var checking = submission.Submission.AutomaticChecking;

			var slide = courseStorage.FindCourse(checking.CourseId)?.FindSlideByIdNotSafe(checking.SlideId) as ExerciseSlide;
			if (slide is null)
			{
				Log.Warn($"Can't find exercise slide {checking.SlideId} in course {checking.CourseId}. Exit");
				return false;
			}

			var score = checking.IsRightAnswer ? 1 : 0;

			var message = checking.IsCompilationError ? checking.CompilationError!.Text : checking.Output!.Text;
			return await client.PutResult(new XQueueResult
			{
				header = submission.XQueueHeader,
				Body = new XQueueResultBody
				{
					IsCorrect = checking.IsRightAnswer,
					Message = message,
					Score = score
				}
			});
		}
	}
}