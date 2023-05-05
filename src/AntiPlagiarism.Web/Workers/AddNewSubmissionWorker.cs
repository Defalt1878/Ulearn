using System;
using System.Threading.Tasks;
using AntiPlagiarism.Web.CodeAnalyzing;
using AntiPlagiarism.Web.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vostok.Applications.Scheduled;
using Vostok.Hosting.Abstractions;
using Vostok.Logging.Abstractions;

namespace AntiPlagiarism.Web.Workers
{
	public class AddNewSubmissionWorker : VostokScheduledApplication
	{
		private readonly TimeSpan sleep = TimeSpan.FromSeconds(5);

		private readonly IServiceScopeFactory serviceScopeFactory;
		private readonly IOptions<AntiPlagiarismConfiguration> configuration;

		private static ILog Log => LogProvider.Get().ForContext(typeof(AddNewSubmissionWorker));

		public AddNewSubmissionWorker(
			IOptions<AntiPlagiarismConfiguration> configuration,
			IServiceScopeFactory serviceScopeFactory)
		{
			this.serviceScopeFactory = serviceScopeFactory;
			this.configuration = configuration;
		}

		public override void Setup(IScheduledActionsBuilder builder, IVostokHostingEnvironment environment)
		{
			RunNewSubmissionWorkers(builder);
		}

		private void RunNewSubmissionWorkers(IScheduledActionsBuilder builder)
		{
			var threadsCount = configuration.Value.AntiPlagiarism.ThreadsCount;
			if (threadsCount < 1)
			{
				Log.Error($"Не могу определить количество потоков для запуска из конфигурации: ${threadsCount}. Количество потоков должно быть положительно");
				throw new ArgumentOutOfRangeException(nameof(threadsCount), @"Number of threads (antiplagiarism:threadsCount) should be positive");
			}

			Log.Info($"Запускаю AddNewSubmissionWorker в {threadsCount} потока(ов)");
			for (var i = 0; i < threadsCount; i++)
			{
				var scheduler = Scheduler.PeriodicalWithConstantPause(sleep);
				builder.Schedule($"AddNewSubmissionWorker #{i}", scheduler, Task);
			}
		}

		private async Task Task()
		{
			while (true)
			{
				var newSubmissionHandled = false;
				await using (var scope = serviceScopeFactory.CreateAsyncScope())
				{
					var newSubmissionHandler = scope.ServiceProvider.GetService<NewSubmissionHandler>();
					try
					{
						newSubmissionHandled = await newSubmissionHandler.HandleNewSubmission();
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Exception during HandleNewSubmission");
					}
				}

				if (!newSubmissionHandled)
					return;
			}
		}
	}
}