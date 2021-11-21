﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntiPlagiarism.Api;
using AntiPlagiarism.Api.Models.Parameters;
using AntiPlagiarism.ConsoleApp.Models;
using AntiPlagiarism.ConsoleApp.SubmissionPreparer;
using Ulearn.Common;

namespace AntiPlagiarism.ConsoleApp
{
	public class ConsoleClient
	{
		private IAntiPlagiarismClient antiPlagiarismClient;
		private SubmissionSearcher submissionSearcher;
		private readonly Repository repository;
		private const int MaxInQuerySubmissionsCount = 5;

		public ConsoleClient(IAntiPlagiarismClient antiPlagiarismClient,
			SubmissionSearcher submissionSearcher,
			Repository repository)
		{
			this.antiPlagiarismClient = antiPlagiarismClient;
			this.submissionSearcher = submissionSearcher;
			this.repository = repository;
		}

		public List<Submission> GetNewSubmissionsAsync()
		{
			return submissionSearcher.GetSubmissions();
		}

		public async Task ShowTaskPlagiarismsAsync(Guid taskId)
		{
			foreach (var submission in repository
				.SubmissionsInfo.Submissions.Where(s => s.TaskId == taskId))
			{
				var response = await antiPlagiarismClient.GetAuthorPlagiarismsAsync(new GetAuthorPlagiarismsParameters
				{
					AuthorId = submission.AuthorId,
					TaskId = submission.TaskId,
					Language = Language.CSharp
				});

				var author = repository.SubmissionsInfo.Authors.First(a => a.Id == submission.AuthorId);
				// todo разобраться что тут писать
				Console.WriteLine($"{author.Name} - ");
			}
		}

		public async Task SendSubmissionsAsync(List<Submission> submissions)
		{
			var remainingSubmissions = new Queue<Submission>(submissions);
			var inQueueSubmissionIds = new List<int>();
			
			while (remainingSubmissions.Any() || inQueueSubmissionIds.Any())
			{
				// Если отправлено больше какого-то предела, узнаем, проверены ли уже отправленные
				// Если еще не проверены - засыпаем на 10 секунд
				if (inQueueSubmissionIds.Count >= MaxInQuerySubmissionsCount)
				{
					var getProcessingStatusResponse = await antiPlagiarismClient
						.GetProcessingStatusAsync(
							new GetProcessingStatusParameters
							{
								SubmissionIds = inQueueSubmissionIds.ToArray()
							});
					inQueueSubmissionIds = getProcessingStatusResponse.InQueueSubmissionIds.ToList();
					
					if (inQueueSubmissionIds.Count >= MaxInQuerySubmissionsCount)
						await Task.Delay(10 * 1000);
				}
				else
				{
					var submission = remainingSubmissions.Dequeue();
					await SendSubmissionAsync(submission);
					inQueueSubmissionIds.Add(submission.Info.SubmissionId);
				}
			}
		}

		private async Task SendSubmissionAsync(Submission submission)
		{
			var response = await antiPlagiarismClient.AddSubmissionAsync(new AddSubmissionParameters
			{
				TaskId = submission.Info.TaskId,
				AuthorId = submission.Info.AuthorId,
				Code = submission.Code,
				Language = Language.CSharp,
				AdditionalInfo = "some important info",
				ClientSubmissionId = "client Id (name + task)"
			});
			submission.Info.SubmissionId = response.SubmissionId;
			
			repository.AddSubmissionInfo(submission.Info);
		}
	}
}