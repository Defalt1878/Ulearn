using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Ulearn.Core.Configuration;
using Vostok.Applications.Scheduled;
using Vostok.Hosting.Abstractions;
using Vostok.Logging.Microsoft;

namespace Ulearn.Common.Api
{
	public abstract class BaseApplication : IVostokApplication
	{
		protected static UlearnConfiguration configuration;
		protected IServiceProvider serviceProvider;

		public virtual Task InitializeAsync(IVostokHostingEnvironment hostingEnvironment)
		{
			var services = new ServiceCollection();
			ConfigureServices(services, hostingEnvironment);
			serviceProvider = services.BuildServiceProvider();
			hostingEnvironment.HostExtensions.AsMutable().Add(serviceProvider); // позволяет запрашивать var serviceProvider = environment.HostExtensions.Get<IServiceProvider>();
			return Task.CompletedTask;
		}

		public abstract Task RunAsync(IVostokHostingEnvironment environment);

		protected virtual void ConfigureServices(IServiceCollection services, IVostokHostingEnvironment hostingEnvironment)
		{
			services.AddLogging(builder => builder.AddVostok(hostingEnvironment.Log));
			configuration = hostingEnvironment.SecretConfigurationProvider.Get<UlearnConfiguration>(hostingEnvironment.SecretConfigurationSource);

			services.Configure<UlearnConfiguration>(options =>
				options.SetFrom(hostingEnvironment.SecretConfigurationProvider.Get<UlearnConfiguration>(hostingEnvironment.SecretConfigurationSource)));

			ConfigureDi(services);
		}

		protected virtual void ConfigureDi(IServiceCollection services)
		{
		}

		public void SetupScheduled<T>(IScheduledActionsBuilder builder, IVostokHostingEnvironment environment)
			where T : VostokScheduledApplication
		{
			// CompositeApplication инициализирует application последовательно, поэтому DI уже будет настроен в InitializeAsync
			var updateCoursesWorker = serviceProvider.GetService<T>();
			updateCoursesWorker.Setup(builder, environment);
		}
	}
}