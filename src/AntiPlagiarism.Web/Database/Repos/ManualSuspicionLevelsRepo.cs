using System;
using System.Threading.Tasks;
using System.Transactions;
using AntiPlagiarism.Web.Database.Extensions;
using AntiPlagiarism.Web.Database.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Ulearn.Common;

namespace AntiPlagiarism.Web.Database.Repos;

public interface IManualSuspicionLevelsRepo
{
	Task SetManualSuspicionLevelsAsync(ManualSuspicionLevels manualSuspicionLevels);
	Task<ManualSuspicionLevels> GetManualSuspicionLevelsAsync(Guid taskId, Language language);
}

public class ManualSuspicionLevelsRepo : IManualSuspicionLevelsRepo
{
	private readonly AntiPlagiarismDb db;

	public ManualSuspicionLevelsRepo(AntiPlagiarismDb db)
	{
		this.db = db;
	}

	public async Task SetManualSuspicionLevelsAsync(ManualSuspicionLevels manualSuspicionLevels)
	{
		var executionStrategy = new NpgsqlRetryingExecutionStrategy(db, 3);
		await executionStrategy.ExecuteAsync(async () =>
		{
			using var ts = new TransactionScope(TransactionScopeOption.Required, TimeSpan.FromSeconds(30), TransactionScopeAsyncFlowOption.Enabled);
			db.AddOrUpdate(manualSuspicionLevels, p => p.TaskId == manualSuspicionLevels.TaskId && p.Language == manualSuspicionLevels.Language);
			await db.SaveChangesAsync().ConfigureAwait(false);
			ts.Complete();
		});
	}

	public async Task<ManualSuspicionLevels> GetManualSuspicionLevelsAsync(Guid taskId, Language language)
	{
		return await db.ManualSuspicionLevels.FindAsync(taskId, language);
	}
}