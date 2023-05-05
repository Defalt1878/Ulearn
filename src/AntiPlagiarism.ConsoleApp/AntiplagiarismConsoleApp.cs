using System;
using System.Collections.Generic;
using System.Linq;
using AntiPlagiarism.ConsoleApp.Models;
using AntiPlagiarism.ConsoleApp.PlagiarismWriter;
using AntiPlagiarism.ConsoleApp.SubmissionPreparer;

namespace AntiPlagiarism.ConsoleApp
{
	public class AntiplagiarismConsoleApp
	{
		private readonly SubmissionSearcher submissionSearcher;
		private readonly SubmissionSender submissionSender;
		private readonly PlagiarismReceiver plagiarismReceiver;
		private readonly Repository repository;

		public AntiplagiarismConsoleApp(SubmissionSearcher submissionSearcher,
			SubmissionSender submissionSender,
			PlagiarismReceiver plagiarismReceiver,
			Repository repository)
		{
			this.submissionSearcher = submissionSearcher;
			this.repository = repository;
			this.plagiarismReceiver = plagiarismReceiver;
			this.submissionSender = submissionSender;
		}

		public void Run()
		{
			try
			{
				SendNewSubmissions();
				GetPlagiarisms();
			}
			catch (Exception exception)
			{
				ConsoleWorker.WriteError(exception);
			}

			Console.ReadKey();
		}

		private List<Submission> GetNewSubmissions()
		{
			return submissionSearcher.GetSubmissions();
		}

		private void SendNewSubmissions()
		{
			var newSubmissions = GetNewSubmissions();
			if (newSubmissions.Count == 0)
			{
				Console.WriteLine("Новых посылок не найдено");
				return;
			}

			if (ValidateSubmissions(newSubmissions))
			{
				submissionSender.SendSubmissionsAsync(newSubmissions).GetAwaiter().GetResult();
				ConsoleWorker.WriteLine("Посылки успешно отправлены");
			}
			else
			{
				ConsoleWorker.WriteLine("Новые посылки не были отправлены");
			}
		}

		private void GetPlagiarisms()
		{
			plagiarismReceiver.GetPlagiarismsAsync().GetAwaiter().GetResult();
		}

		private bool ValidateSubmissions(IEnumerable<Submission> submissions)
		{
			Console.WriteLine("Будут отправлены следующие посылки:");
			ConsoleWorker.PrintSubmissions(submissions.ToArray(),
				repository.SubmissionsInfo.Authors.ToArray(),
				repository.SubmissionsInfo.Tasks.ToArray());

			Console.Write("Отправить посылки? ");
			return ConsoleWorker.GetUserAnswer();
		}
	}
}