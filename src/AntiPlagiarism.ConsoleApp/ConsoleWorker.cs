using System;
using System.Collections.Generic;
using System.Linq;
using AntiPlagiarism.ConsoleApp.Models;
using Vostok.Logging.Abstractions;

namespace AntiPlagiarism.ConsoleApp
{
	public static class ConsoleWorker
	{
		private static ILog Log => LogProvider.Get();

		public static void WriteLine(string text)
		{
			Console.WriteLine(text);
			Log.Info(text);
		}

		public static void ReWriteLine(string text)
		{
			Console.Write($"\r{text}");
		}

		public static void WriteError(Exception exception, bool printToUser = true)
		{
			if (printToUser)
			{
				Console.WriteLine();
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"Произошла ошибка {exception.Message}. Подробности смотрите в логе");
				Console.ResetColor();
			}

			Log.Error(exception);
		}

		public static bool GetUserAnswer()
		{
			var answer = "";
			while (answer != "no" && answer != "yes")
			{
				Console.Write("(yes/no): ");
				answer = Console.ReadLine();
			}

			return answer == "yes";
		}

		public static string GetUserInput()
		{
			Console.Write("> ");
			return Console.ReadLine();
		}

		public static void PrintSubmissions(IEnumerable<Submission> submissions, Author[] authors, TaskInfo[] tasks)
		{
			foreach (var taskToSubmissions in submissions
						.GroupBy(s => s.Info.TaskId))
			{
				var task = tasks.First(t => t.Id == taskToSubmissions.Key).Title;
				Console.WriteLine($"Task {task}");
				foreach (var submission in taskToSubmissions)
				{
					var author = authors.First(a => a.Id == submission.Info.AuthorId);
					Console.WriteLine($"->  {author.Name}");
				}

				Console.WriteLine();
			}
		}
	}
}