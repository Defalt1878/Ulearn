using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntiPlagiarism.Api;
using AntiPlagiarism.Api.Models.Parameters;
using AntiPlagiarism.ConsoleApp.Models;
using Vostok.Logging.Abstractions;

namespace AntiPlagiarism.ConsoleApp.PlagiarismWriter
{
	public class PlagiarismReceiver
	{
		private readonly ILog log = LogProvider.Get();
		private readonly IAntiPlagiarismClient antiPlagiarismClient;
		private readonly DiffsWriter diffsWriter;
		private readonly Repository repository;

		public PlagiarismReceiver(
			IAntiPlagiarismClient antiPlagiarismClient,
			DiffsWriter diffsWriter,
			Repository repository)
		{
			this.antiPlagiarismClient = antiPlagiarismClient;
			this.diffsWriter = diffsWriter;
			this.repository = repository;
		}

		public async Task GetPlagiarismsAsync()
		{
			var plagiarisms = new List<PlagiarismInfo>();
			ConsoleWorker.WriteLine("Получение данных о плагиате");
			foreach (var submission in repository.SubmissionsInfo.Submissions)
			{
				log.Info("Get AntiPlagiarism levels");
				var response = await antiPlagiarismClient.GetSubmissionPlagiarismsAsync(new GetSubmissionPlagiarismsParameters
				{
					SubmissionId = submission.SubmissionId
				});

				var author = repository.SubmissionsInfo.Authors.First(a => a.Id == submission.AuthorId);
				var task = repository.SubmissionsInfo.Tasks.First(t => t.Id == submission.TaskId);
				if (!response.Plagiarisms.Any())
					continue;

				var mostSimilarSubmission = response.Plagiarisms.OrderByDescending(p => p.Weight).First();

				var authorOfMostSimilarSubmission = repository.SubmissionsInfo.Authors.Single(
					a => a.Id == mostSimilarSubmission.SubmissionInfo.AuthorId);
				var levels = await antiPlagiarismClient.GetSuspicionLevelsAsync(new GetSuspicionLevelsParameters
				{
					TaskId = submission.TaskId,
					Language = submission.Language
				});

				var weight = response.Plagiarisms.Max(p => p.Weight);
				if (weight < levels.SuspicionLevels.FaintSuspicion)
					continue;

				plagiarisms.Add(new PlagiarismInfo
				{
					AuthorName = author.Name,
					TaskTitle = task.Title,
					PlagiarismAuthorName = authorOfMostSimilarSubmission.Name,
					SuspicionLevel = weight < levels.SuspicionLevels.StrongSuspicion ? "слабый" : "сильный",
					Language = submission.Language,
					Weight = $"{Math.Round(weight * 100, 2)}%",
					Code = response.SubmissionInfo.Code,
					PlagiarismCode = mostSimilarSubmission.SubmissionInfo.Code
				});
			}

			diffsWriter.WritePlagiarisms(plagiarisms
				.Select(p1 =>
				{
					p1.PlagiarismCount = plagiarisms.Count(p2 => p1.AuthorName == p2.AuthorName);
					return p1;
				})
				.OrderByDescending(p => p.PlagiarismCount)
				.ToList()
			);
		}
	}
}