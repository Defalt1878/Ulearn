using System;
using System.Threading.Tasks;
using Database.Models;
using Database.Repos;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.RunCheckerJobApi;
using Ulearn.Web.Api.Utils.LTI;
using Vostok.Logging.Abstractions;

namespace Ulearn.Web.Api.Controllers.Runner
{
	public class LtiResultObserver : IResultObserver
	{
		private readonly ICourseStorage courseStorage;
		private readonly ILtiConsumersRepo ltiConsumersRepo;
		private readonly ILtiRequestsRepo ltiRequestsRepo;
		private readonly IVisitsRepo visitsRepo;

		public LtiResultObserver(ICourseStorage courseStorage, ILtiConsumersRepo ltiConsumersRepo,
			ILtiRequestsRepo ltiRequestsRepo, IVisitsRepo visitsRepo)
		{
			this.courseStorage = courseStorage;
			this.ltiConsumersRepo = ltiConsumersRepo;
			this.ltiRequestsRepo = ltiRequestsRepo;
			this.visitsRepo = visitsRepo;
		}

		private static ILog Log => LogProvider.Get().ForContext(typeof(LtiResultObserver));

		public async Task ProcessResult(UserExerciseSubmission submission, RunningResults result)
		{
			var ltiRequestJson = await ltiRequestsRepo.Find(submission.CourseId, submission.UserId, submission.SlideId);
			if (ltiRequestJson is not null)
				try
				{
					var exerciseSlide = courseStorage.FindCourse(submission.CourseId).FindSlideByIdNotSafe(submission.SlideId) as ExerciseSlide;
					var score = await visitsRepo.GetScore(submission.CourseId, submission.SlideId, submission.UserId);
					await LtiUtils.SubmitScore(exerciseSlide, submission.UserId, score, ltiRequestJson, ltiConsumersRepo);
				}
				catch (Exception e)
				{
					Log.Error(e, "Мы не смогли отправить баллы на образовательную платформу");
				}
		}
	}
}