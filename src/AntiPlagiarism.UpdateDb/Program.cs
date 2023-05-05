using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntiPlagiarism.UpdateDb.Configuration;
using AntiPlagiarism.Web.CodeAnalyzing;
using AntiPlagiarism.Web.Configuration;
using AntiPlagiarism.Web.Database;
using AntiPlagiarism.Web.Database.Repos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ulearn.Core.Configuration;
using Ulearn.Core.Logging;

namespace AntiPlagiarism.UpdateDb;

public static class Program
{
	public static void Main(string[] args)
	{
		RunAsync(args).Wait();
	}

	private static async Task RunAsync(IReadOnlyList<string> args)
	{
		Console.WriteLine(
			@"This tool will help you to update antiplagiarism database. " +
			@"I.e. in case when new logic for code units extraction have been added."
		);

		var firstSubmissionId = 0;
		if (args.Contains("--start"))
		{
			var startArgIndex = args.FindIndex("--start");
			if (startArgIndex + 1 >= args.Count || !int.TryParse(args[startArgIndex + 1], out firstSubmissionId))
				firstSubmissionId = 0;
		}

		var updateOnlyTokensCount = args.Contains("--update-tokens-count");

		var provider = GetServiceProvider();
		var updater = provider.GetService<AntiPlagiarismSnippetsUpdater>();
		await updater.UpdateAsync(firstSubmissionId, updateOnlyTokensCount).ConfigureAwait(false);
	}

	private static ServiceProvider GetServiceProvider()
	{
		var configuration = ApplicationConfiguration.Read<AntiPlagiarismUpdateDbConfiguration>();

		var services = new ServiceCollection();

		services.AddOptions();

		services.Configure<AntiPlagiarismUpdateDbConfiguration>(ApplicationConfiguration.GetConfiguration());
		services.Configure<AntiPlagiarismConfiguration>(ApplicationConfiguration.GetConfiguration());

		LoggerSetup.Setup(configuration.HostLog, configuration.GraphiteServiceName);
		services.AddScoped(_ => GetDatabase(configuration));
		services.AddScoped<AntiPlagiarismSnippetsUpdater>();
		services.AddScoped<ISnippetsRepo, SnippetsRepo>();
		services.AddScoped<ISubmissionsRepo, SubmissionsRepo>();
		services.AddSingleton<CSharpCodeUnitsExtractor>();
		services.AddSingleton<CodeUnitsExtractor>();
		services.AddSingleton<TokensExtractor>();
		services.AddSingleton<SnippetsExtractor>();
		services.AddSingleton<SubmissionSnippetsExtractor>();

		return services.BuildServiceProvider();
	}

	private static AntiPlagiarismDb GetDatabase(AntiPlagiarismUpdateDbConfiguration configuration)
	{
		var optionsBuilder = new DbContextOptionsBuilder<AntiPlagiarismDb>();
		optionsBuilder.UseNpgsql(configuration.Database, o => o.SetPostgresVersion(13, 2));

		return new AntiPlagiarismDb(optionsBuilder.Options);
	}
}