using System;
using System.Threading.Tasks;
using Database.Repos;
using Microsoft.Extensions.DependencyInjection;
using Vostok.Applications.Scheduled;
using Vostok.Hosting.Abstractions;
using Vostok.Logging.Abstractions;

namespace Ulearn.Web.Api.Workers;

public class RefreshMaterializedViewsWorker : VostokScheduledApplication
{
	private readonly IServiceScopeFactory serviceScopeFactory;

	public RefreshMaterializedViewsWorker(IServiceScopeFactory serviceScopeFactory)
	{
		this.serviceScopeFactory = serviceScopeFactory;
	}

	private static ILog Log => LogProvider.Get().ForContext(typeof(RefreshMaterializedViewsWorker));

	public override void Setup(IScheduledActionsBuilder builder, IVostokHostingEnvironment environment)
	{
		var scheduler = Scheduler.Multi(Scheduler.Periodical(TimeSpan.FromMinutes(10)), Scheduler.OnDemand(out var runRefreshExerciseStatisticsMaterializedViews));
		builder.Schedule("RefreshExerciseStatisticsMaterializedView", scheduler, RefreshExerciseStatisticsMaterializedViews);

		runRefreshExerciseStatisticsMaterializedViews();
	}

	private async Task RefreshExerciseStatisticsMaterializedViews()
	{
		Log.Info("RefreshExerciseStatisticsMaterializedViews");
		using (var scope = serviceScopeFactory.CreateScope())
		{
			var slideCheckingsRepo = scope.ServiceProvider.GetService<ISlideCheckingsRepo>();
			await slideCheckingsRepo!.RefreshExerciseStatisticsMaterializedViews();
		}

		Log.Info("End RefreshExerciseStatisticsMaterializedViews");
	}
}