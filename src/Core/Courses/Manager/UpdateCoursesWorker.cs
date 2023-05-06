using System;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Applications.Scheduled;
using Vostok.Hosting.Abstractions;

namespace Ulearn.Core.Courses.Manager
{
	public class UpdateCoursesWorker : VostokScheduledApplication
	{
		public const string UpdateCoursesJobName = "UpdateCoursesJob";
		public const string UpdateTempCoursesJobName = "UpdateTempCoursesJob";
		private readonly TimeSpan coursesUpdatePeriod = TimeSpan.FromMilliseconds(1000);
		private readonly ICourseUpdater courseUpdater;
		private readonly TimeSpan tempCoursesUpdatePeriod = TimeSpan.FromMilliseconds(500);

		public UpdateCoursesWorker(ICourseUpdater courseUpdater)
		{
			this.courseUpdater = courseUpdater;
		}

		public override void Setup(IScheduledActionsBuilder builder, IVostokHostingEnvironment environment)
		{
			RunUpdateCoursesWorker(builder);
		}

		private void RunUpdateCoursesWorker(IScheduledActionsBuilder builder)
		{
			var updateCoursesScheduler = Scheduler.Multi(Scheduler.Periodical(coursesUpdatePeriod), Scheduler.OnDemand(out _));
			builder.Schedule(UpdateCoursesJobName, updateCoursesScheduler, courseUpdater.UpdateCoursesAsync);

			var updateTempCoursesScheduler = Scheduler.Multi(Scheduler.Periodical(tempCoursesUpdatePeriod), Scheduler.OnDemand(out var updateTempCourses));
			builder.Schedule(UpdateTempCoursesJobName, updateTempCoursesScheduler, courseUpdater.UpdateTempCoursesAsync);

			courseUpdater.UpdateCoursesAsync().Wait(); // в этом потоке
			updateTempCourses(); // в другом потоке
		}

		public async Task DoInitialCourseLoadAndRunCoursesUpdateInThreads()
		{
			await courseUpdater.UpdateCoursesAsync();
			var coursesThread = new Thread(UpdateCoursesLoop);
			coursesThread.Start();
			var tempCoursesThread = new Thread(UpdateTempCoursesLoop);
			tempCoursesThread.Start();
		}

		private async void UpdateCoursesLoop()
		{
			while (true)
			{
				await courseUpdater.UpdateCoursesAsync();
				await Task.Delay(coursesUpdatePeriod);
			}
			// ReSharper disable once FunctionNeverReturns
		}

		private async void UpdateTempCoursesLoop()
		{
			while (true)
			{
				await courseUpdater.UpdateTempCoursesAsync();
				await Task.Delay(tempCoursesUpdatePeriod);
			}
			// ReSharper disable once FunctionNeverReturns
		}
	}
}