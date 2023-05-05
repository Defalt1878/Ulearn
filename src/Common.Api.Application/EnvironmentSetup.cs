using System;
using Ulearn.Core.Configuration;
using Ulearn.Core.Logging;
using Vostok.Configuration;
using Vostok.Configuration.Abstractions;
using Vostok.Configuration.Sources;
using Vostok.Configuration.Sources.CommandLine;
using Vostok.Configuration.Sources.Json;
using Vostok.Hosting.Kontur;
using Vostok.Hosting.Setup;
using Vostok.Logging.Abstractions;

namespace Ulearn.Common.Api;

public class EnvironmentSetupBuilder
{
	private readonly string application;
	private readonly string[] commandLineArguments;

	public EnvironmentSetupBuilder(string application, string[] commandLineArguments)
	{
		this.application = application;
		this.commandLineArguments = commandLineArguments;
	}

	public void EnvironmentSetup(IVostokHostingEnvironmentBuilder builder)
	{
		var configurationProvider = new ConfigurationProvider(new ConfigurationProviderSettings());
		var configurationSource = GetConfigurationSource();
		configurationProvider.SetupSourceFor<UlearnConfiguration>(configurationSource);
		var ulearnConfiguration = configurationProvider.Get<UlearnConfiguration>();
		var environment = ulearnConfiguration.Environment ?? "dev";

		if (!ulearnConfiguration.DisableKonturServices)
			builder = builder.SetupForKontur();

		builder.SetupApplicationIdentity(identityBuilder => identityBuilder
				.SetProject("ulearn")
				.SetApplication(application)
				.SetEnvironment(environment)
				.SetInstance(Environment.MachineName.Replace(".", "_").ToLower()))
			.SetupConfiguration(configurationBuilder => configurationBuilder.AddSecretSource(configurationSource));
		if (ulearnConfiguration.Port != null)
			builder.SetPort(ulearnConfiguration.Port.Value);
		if (ulearnConfiguration.BaseUrl != null)
			builder.SetBaseUrlPath(ulearnConfiguration.BaseUrl);
		builder
			.DisableServiceBeacon()
			.SetupHerculesSink(sinkBuilder => SetupHerculesSink(sinkBuilder, ulearnConfiguration))
			.SetupLog((logBuilder, _) => SetupLog(logBuilder, ulearnConfiguration));

		if (ulearnConfiguration.ForceHttps ?? false)
			builder.SetHttpsScheme();
	}

	private static void SetupHerculesSink(IVostokHerculesSinkBuilder sinkBuilder, UlearnConfiguration ulearnConfiguration)
	{
		if (ulearnConfiguration.Hercules == null || string.IsNullOrEmpty(ulearnConfiguration.Hercules.ApiKey))
		{
			sinkBuilder.Disable();
			return;
		}

		sinkBuilder.SetApiKeyProvider(() => ulearnConfiguration.Hercules.ApiKey);
	}

	private static void SetupLog(IVostokCompositeLogBuilder logBuilder, UlearnConfiguration ulearnConfiguration)
	{
		var log = LoggerSetup.Setup(ulearnConfiguration.HostLog, ulearnConfiguration.GraphiteServiceName, false);
		logBuilder.AddLog(log);

		var (minimumLevel, dbMinimumLevel) = LoggerSetup.GetMinimumLevels(ulearnConfiguration.HostLog);
		var min = dbMinimumLevel > minimumLevel ? minimumLevel : dbMinimumLevel;

		logBuilder.SetupHerculesLog(herculesLogBuilder =>
		{
			herculesLogBuilder.SetStream(ulearnConfiguration.Hercules.Stream);
			herculesLogBuilder.SetApiKeyProvider(() => ulearnConfiguration.Hercules.ApiKey);
			herculesLogBuilder.CustomizeLog(l =>
				LoggerSetup.FilterLogs(l, minimumLevel, dbMinimumLevel)
					.WithMinimumLevel(min)
			);
		});
	}

	private IConfigurationSource GetConfigurationSource()
	{
		var configurationSource = new CommandLineSource(commandLineArguments)
			.CombineWith(new JsonFileSource("appsettings.json"))
			.CombineWith(new JsonFileSource("appsettings." + (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production") + ".json"));
		var environmentName = Environment.GetEnvironmentVariable("UlearnEnvironmentName");
		if (environmentName != null && environmentName.ToLower().Contains("local"))
			configurationSource = configurationSource.CombineWith(new JsonFileSource("appsettings.local.json"));
		return configurationSource;
	}
}