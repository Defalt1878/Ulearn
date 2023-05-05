using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntiPlagiarism.Api;
using AntiPlagiarism.Api.Models.Parameters;
using AntiPlagiarism.ConsoleApp.Models;
using Vostok.Logging.Abstractions;

namespace AntiPlagiarism.ConsoleApp;

public class SubmissionSender
{
	private readonly ILog log = LogProvider.Get();
	private readonly IAntiPlagiarismClient antiPlagiarismClient;
	private readonly Repository repository;

	public SubmissionSender(IAntiPlagiarismClient antiPlagiarismClient, Repository repository)
	{
		this.antiPlagiarismClient = antiPlagiarismClient;
		this.repository = repository;
	}

	public async Task SendSubmissionsAsync(List<Submission> submissions)
	{
		var remainingSubmissions = new Queue<Submission>(submissions);
		var inQueueSubmissionIds = new List<int>();

		log.Info("Send submissions to AntiPlagiarism");
		Console.WriteLine("Отправка решений на проверку АнтиПлагиатом. Это может занять несколько минут	");

		while (remainingSubmissions.Any() || inQueueSubmissionIds.Any())
		{
			var processedSubmissionsCount = submissions.Count - (remainingSubmissions.Count + inQueueSubmissionIds.Count);

			while (remainingSubmissions.Any() && inQueueSubmissionIds.Count < Config.MaxInQuerySubmissionsCount)
			{
				var submission = remainingSubmissions.Dequeue();
				await SendSubmissionAsync(submission);
				inQueueSubmissionIds.Add(submission.Info.SubmissionId);
			}

			// Узнаем, проверены ли уже отправленные
			// Если еще не проверены - засыпаем на 5 секунд
			if (inQueueSubmissionIds.Any())
			{
				var getProcessingStatusResponse = await antiPlagiarismClient
					.GetProcessingStatusAsync(
						new GetProcessingStatusParameters
						{
							SubmissionIds = inQueueSubmissionIds.ToArray()
						});
				inQueueSubmissionIds = getProcessingStatusResponse.InQueueSubmissionIds.ToList();

				if (inQueueSubmissionIds.Count > 0)
					await Task.Delay(5 * 1000);
			}

			ConsoleWorker.ReWriteLine($"Обработано {processedSubmissionsCount}/{submissions.Count} решений");
		}

		ConsoleWorker.ReWriteLine("Обработка решений завершена.");
		Console.WriteLine();
	}

	private async Task SendSubmissionAsync(Submission submission)
	{
		var authorName = repository.SubmissionsInfo.Authors.First(a => a.Id == submission.Info.AuthorId).Name;
		var taskTitle = repository.SubmissionsInfo.Tasks.First(t => t.Id == submission.Info.TaskId).Title;

		var response = await antiPlagiarismClient.AddSubmissionAsync(new AddSubmissionParameters
		{
			TaskId = submission.Info.TaskId,
			AuthorId = submission.Info.AuthorId,
			Code = submission.Code,
			Language = submission.Info.Language,
			AdditionalInfo = $"Task: {taskTitle}; Author: {authorName}",
			ClientSubmissionId = "client Id (name + task)"
		});
		submission.Info.SubmissionId = response.SubmissionId;

		log.Info($"Send submission {submission.Info.SubmissionId}");
		repository.AddSubmissionInfo(submission.Info);
	}
}