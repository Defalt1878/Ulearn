using System;
using System.Linq;
using System.Threading.Tasks;
using Database.Repos;
using Google;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ulearn.Core.Configuration;
using Ulearn.Core.GoogleSheet;
using Ulearn.Web.Api.Models.Parameters.Analytics;
using Ulearn.Web.Api.Utils;
using Vostok.Applications.Scheduled;
using Vostok.Hosting.Abstractions;
using Vostok.Logging.Abstractions;
using Web.Api.Configuration;

namespace Ulearn.Web.Api.Workers
{
	public class RefreshGoogleSheetWorker : VostokScheduledApplication
	{
		private readonly UlearnConfiguration configuration;
		private readonly IServiceScopeFactory serviceScopeFactory;
		private readonly StatisticModelUtils statisticModelUtils;

		public RefreshGoogleSheetWorker(IServiceScopeFactory serviceScopeFactory, IOptions<WebApiConfiguration> options,
			StatisticModelUtils statisticModelUtils)
		{
			this.serviceScopeFactory = serviceScopeFactory;
			configuration = options.Value;
			this.statisticModelUtils = statisticModelUtils;
		}

		private static ILog Log => LogProvider.Get().ForContext(typeof(RefreshGoogleSheetWorker));

		public override void Setup(IScheduledActionsBuilder builder, IVostokHostingEnvironment environment)
		{
			var scheduler = Scheduler.Periodical(TimeSpan.FromMinutes(1));
			builder.Schedule("RefreshGoogleSheets", scheduler, RefreshGoogleSheets);
		}

		private async Task RefreshGoogleSheets()
		{
			Log.Info("RefreshGoogleSheets started");
			using (var scope = serviceScopeFactory.CreateScope())
			{
				var googleSheetExportTasksRepo = scope.ServiceProvider.GetService<IGoogleSheetExportTasksRepo>();
				var timeNow = DateTime.UtcNow;
				var tasks = (await googleSheetExportTasksRepo.GetAllTasks())
					.Where(t =>
						t.RefreshStartDate is not null
						&& t.RefreshStartDate.Value <= timeNow
						&& t.RefreshEndDate is not null
						&& t.RefreshEndDate.Value >= timeNow);
				foreach (var task in tasks)
				{
					if (task.LastUpdateDate is not null && (timeNow - task.LastUpdateDate.Value).TotalMinutes < task.RefreshTimeInMinutes)
						continue;

					Log.Info($"Start refreshing task {task.Id}");
					var courseStatisticsParams = new CourseStatisticsParams
					{
						CourseId = task.CourseId,
						ListId = task.ListId,
						GroupsIds = task.Groups.Select(g => g.GroupId.ToString()).ToList()
					};

					string exceptionMessage = null;
					try
					{
						var sheet = await statisticModelUtils.GetFilledGoogleSheetModel(courseStatisticsParams, 3000, task.AuthorId, timeNow);
						var credentialsJson = configuration.GoogleAccessCredentials;
						var client = new GoogleApiClient(credentialsJson);
						client.FillSpreadSheet(task.SpreadsheetId, sheet);
					}
					catch (GoogleApiException e)
					{
						exceptionMessage = $"Google Api ERROR: {e.Error.Code} {e.Error.Message}";
					}
					catch (Exception e)
					{
						exceptionMessage = e.Message;
					}

					if (exceptionMessage is not null)
						Log.Warn($"Error while filling spread sheet for task {task.Id}, error message {exceptionMessage}");

					await googleSheetExportTasksRepo.SaveTaskUploadResult(task, timeNow, exceptionMessage);

					Log.Info($"Ended refreshing task {task.Id}");
				}
			}

			Log.Info("RefreshGoogleSheets ended");
		}
	}
}