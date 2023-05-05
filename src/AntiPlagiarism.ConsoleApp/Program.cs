using System;
using System.IO;
using AntiPlagiarism.Api;
using AntiPlagiarism.ConsoleApp.Models;
using AntiPlagiarism.ConsoleApp.PlagiarismWriter;
using AntiPlagiarism.ConsoleApp.SubmissionPreparer;
using Ulearn.Common.Extensions;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.Formatting;

namespace AntiPlagiarism.ConsoleApp;

internal static class Program
{
	private static Repository repository;
	private static AntiplagiarismConsoleApp app;
	private static IAntiPlagiarismClient antiPlagiarismClient;
	private static string workingDirectory;

	private static void Main()
	{
		InitLogger();
		InitApp();

		app.Run();
	}

	private static void InitApp()
	{
		workingDirectory = GetWorkingDirectory();
		repository = new Repository(workingDirectory);
		antiPlagiarismClient = new AntiPlagiarismClient(Config.EndPointUrl, GetToken());

		app = new AntiplagiarismConsoleApp(
			new SubmissionSearcher(workingDirectory, new CodeExtractor(repository), repository),
			new SubmissionSender(antiPlagiarismClient, repository),
			new PlagiarismReceiver(antiPlagiarismClient, new DiffsWriter(workingDirectory), repository),
			repository);
	}

	private static string GetWorkingDirectory()
	{
		if (File.Exists(Directory.GetCurrentDirectory()
				.PathCombine(Repository.AntiplagiarismDataDirectory)
				.PathCombine(Repository.ConfigFileName)))
			return Directory.GetCurrentDirectory();
		Console.WriteLine("Введите путь до корневой директории, содержащей решения задач");
		return ConsoleWorker.GetUserInput();
	}

	private static string GetToken()
	{
		if (repository.Config.Token == null)
		{
			Console.WriteLine("Введите токен клиента антиплагиата");
			repository.SetAccessToken(ConsoleWorker.GetUserInput());
		}

		ConsoleWorker.WriteLine($"Используется токен клиента {repository.Config.Token}");
		return repository.Config.Token;
	}

	private static void InitLogger()
	{
		var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "{RollingSuffix}.log");
		var fileLogSettings = new FileLogSettings
		{
			FilePath = logPath,
			RollingStrategy = new RollingStrategyOptions
			{
				MaxFiles = 0,
				Type = RollingStrategyType.Hybrid,
				Period = RollingPeriod.Day,
				MaxSize = 4 * 1073741824L
			},
			OutputTemplate = OutputTemplate.Parse("{Timestamp:HH:mm:ss.fff} {Level:u5} {sourceContext:w}{Message}{NewLine}{Exception}")
		};
		var fileLog = new FileLog(fileLogSettings).WithMinimumLevel(LogLevel.Info);
		LogProvider.Configure(fileLog);
	}
}