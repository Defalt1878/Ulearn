using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using AntiPlagiarism.Web.Database.Extensions;
using AntiPlagiarism.Web.Database.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Ulearn.Common;
using Z.EntityFramework.Plus;

namespace AntiPlagiarism.Web.Database.Repos;

public interface ITasksRepo
{
	Task<List<Guid>> GetTaskIds();
	Task<TaskStatisticsParameters> FindTaskStatisticsParametersAsync(Guid taskId, Language language);
	Task SaveTaskStatisticsParametersAsync(TaskStatisticsParameters parameters, List<TaskStatisticsSourceData> sourceData);
}

public class TasksRepo : ITasksRepo
{
	private readonly AntiPlagiarismDb db;

	public TasksRepo(AntiPlagiarismDb db)
	{
		this.db = db;
	}

	public Task<List<Guid>> GetTaskIds()
	{
		return db.TasksStatisticsParameters.Select(p => p.TaskId).ToListAsync();
	}

	public async Task<TaskStatisticsParameters> FindTaskStatisticsParametersAsync(Guid taskId, Language language)
	{
		return await db.TasksStatisticsParameters.FindAsync(taskId, language);
	}

	/* It's very important that SaveTaskStatisticsParametersAsync() works with disabled EF's Change Tracker */
	public async Task SaveTaskStatisticsParametersAsync(TaskStatisticsParameters parameters, List<TaskStatisticsSourceData> sourceData)
	{
		var executionStrategy = new NpgsqlRetryingExecutionStrategy(db, 5);
		await executionStrategy.ExecuteAsync(async () =>
		{
			using var ts = new TransactionScope(TransactionScopeOption.Required, TimeSpan.FromSeconds(30), TransactionScopeAsyncFlowOption.Enabled);
			await db.TaskStatisticsSourceData
				.Where(d => d.Submission1.TaskId == parameters.TaskId && d.Submission1.Language == parameters.Language)
				.DeleteAsync();
			db.AddOrUpdate(parameters, p => p.TaskId == parameters.TaskId && p.Language == parameters.Language);
			db.TaskStatisticsSourceData.AddRange(sourceData);
			await db.SaveChangesAsync().ConfigureAwait(false);
			ts.Complete();
		});
	}
}