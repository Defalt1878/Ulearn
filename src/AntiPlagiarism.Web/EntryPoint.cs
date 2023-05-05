using System;
using System.Threading.Tasks;
using Ulearn.Common.Api;
using Vostok.Hosting;

namespace AntiPlagiarism.Web;

public static class EntryPoint
{
	public static async Task Main(string[] args)
	{
		AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
		var application = new WebApplication();
		var setupBuilder = new EnvironmentSetupBuilder("antiplagiarism-web", args);
		var hostSettings = new VostokHostSettings(application, setupBuilder.EnvironmentSetup);
		var host = new VostokHost(hostSettings);
		await host.WithConsoleCancellation().RunAsync();
	}
}