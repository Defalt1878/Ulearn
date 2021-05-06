﻿using System;
using AntiPlagiarism.Web.Configuration;
using AntiPlagiarism.Web.Database.Repos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vostok.Applications.Scheduled;
using Vostok.Hosting.Abstractions;
using Vostok.Logging.Abstractions;

namespace AntiPlagiarism.Web.Workers
{
	public class UpdateOldSubmissionsFromStatisticsWorker : VostokScheduledApplication
	{
		private readonly IServiceScopeFactory serviceScopeFactory;
		private readonly AntiPlagiarismConfiguration configuration;

		private static ILog log => LogProvider.Get().ForContext(typeof(UpdateOldSubmissionsFromStatisticsWorker));
// ReSharper disable once NotAccessedField.Local

		public UpdateOldSubmissionsFromStatisticsWorker(
			IOptions<AntiPlagiarismConfiguration> configuration,
			IServiceScopeFactory serviceScopeFactory)
		{
			this.serviceScopeFactory = serviceScopeFactory;
			this.configuration = configuration.Value;
		}

		public override void Setup(IScheduledActionsBuilder builder, IVostokHostingEnvironment environment)
		{
			RunUpdateOldSubmissionsFromStatisticsWorker(builder);
		}

		private void RunUpdateOldSubmissionsFromStatisticsWorker(IScheduledActionsBuilder builder)
		{
			var startTime = configuration.AntiPlagiarism.Actions.UpdateOldSubmissionsFromStatistics.StartTime;
			if (string.IsNullOrEmpty(startTime))
				return;
			var scheduler = Scheduler.Crontab(startTime);
			builder.Schedule("UpdateOldSubmissionsFromStatisticsWorker", scheduler, Task);
		}

		private void Task(object stateInfo)
		{
			using (var scope = serviceScopeFactory.CreateScope())
			{
				var snippetsRepo = scope.ServiceProvider.GetService<ISnippetsRepo>();
				try
				{
					var now = DateTime.Now;
					var submissionInfluenceLimitInMonths = configuration.AntiPlagiarism.SubmissionInfluenceLimitInMonths;
					var border = snippetsRepo.GetOldSubmissionsInfluenceBorderAsync().Result;
					var from = border?.Date ?? new DateTime(2000, 1, 1);
					var to = now.AddMonths(-submissionInfluenceLimitInMonths);
					snippetsRepo.UpdateOldSnippetsStatisticsAsync(from, to).Wait();
					snippetsRepo.SetOldSubmissionsInfluenceBorderAsync(to).Wait();
				}
				catch (Exception ex)
				{
					log.Error(ex, "Exception during UpdateOldSubmissionsFromStatistics");
				}
			}
		}
	}
}