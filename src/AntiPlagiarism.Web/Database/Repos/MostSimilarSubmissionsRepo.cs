using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using AntiPlagiarism.Api.Models.Results;
using AntiPlagiarism.Web.Database.Extensions;
using AntiPlagiarism.Web.Database.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Ulearn.Common;
using Vostok.Logging.Abstractions;

namespace AntiPlagiarism.Web.Database.Repos;

public interface IMostSimilarSubmissionsRepo
{
	Task TrySaveMostSimilarSubmissionAsync(MostSimilarSubmission mostSimilarSubmission);
	Task<List<MostSimilarSubmissions>> GetMostSimilarSubmissionsByTaskAsync(int clientId, Guid taskId, Language language);
}

public class MostSimilarSubmissionsRepo : IMostSimilarSubmissionsRepo
{
	private readonly AntiPlagiarismDb db;

	public MostSimilarSubmissionsRepo(AntiPlagiarismDb db)
	{
		this.db = db;
	}

	private static ILog Log => LogProvider.Get().ForContext(typeof(MostSimilarSubmissionsRepo));

	public async Task TrySaveMostSimilarSubmissionAsync(MostSimilarSubmission mostSimilarSubmission)
	{
		try
		{
			var executionStrategy = new NpgsqlRetryingExecutionStrategy(db, 3);
			await executionStrategy.ExecuteAsync(async () =>
			{
				using var ts = new TransactionScope(TransactionScopeOption.Required, TimeSpan.FromSeconds(30), TransactionScopeAsyncFlowOption.Enabled);
				db.AddOrUpdate(mostSimilarSubmission, p => p.SubmissionId == mostSimilarSubmission.SubmissionId);
				await db.SaveChangesAsync().ConfigureAwait(false);
				ts.Complete();
				return 0;
			});
		}
		catch (InvalidOperationException ex)
		{
			Log.Warn(ex);
		}
	}

	public async Task<List<MostSimilarSubmissions>> GetMostSimilarSubmissionsByTaskAsync(int clientId, Guid taskId, Language language)
	{
		using var scope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }, TransactionScopeAsyncFlowOption.Enabled);
		var resultsWithRepeatingAuthors = await db.MostSimilarSubmissions
			.Where(s => s.Submission.ClientId == clientId && s.Submission.TaskId == taskId && s.Submission.Language == language)
			.Select(s => new
			{
				SubmissionId = s.Submission.ClientSubmissionId,
				SimilarSubmissionId = s.SimilarSubmission.ClientSubmissionId,
				s.Weight,
				s.Submission.AuthorId
			})
			.ToListAsync();
		scope.Complete();

		return resultsWithRepeatingAuthors
			.GroupBy(t => t.AuthorId)
			.Select(g =>
			{
				var max = g.MaxBy(s => s.Weight);
				return new MostSimilarSubmissions
				{
					SubmissionId = max.SubmissionId,
					SimilarSubmissionId = max.SimilarSubmissionId,
					Weight = max.Weight
				};
			}).ToList();
	}
}