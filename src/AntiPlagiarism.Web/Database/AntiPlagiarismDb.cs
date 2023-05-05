using AntiPlagiarism.Web.Database.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.Logging;
using Ulearn.Core.Configuration;

namespace AntiPlagiarism.Web.Database
{
	public class AntiPlagiarismDb : DbContext
	{
		public const string DefaultSchema = "antiplagiarism";

		static AntiPlagiarismDb()
		{
			var configuration = ApplicationConfiguration.Read<UlearnConfiguration>();
			if (configuration.HostLog is not null)
			{
				NpgsqlLogManager.Provider = new AntiPlagiarismDbLoggingProvider();
				NpgsqlLogManager.IsParameterLoggingEnabled = true;
			}
		}

		public AntiPlagiarismDb(DbContextOptions<AntiPlagiarismDb> options)
			: base(options)
		{
		}


		public DbSet<Client> Clients { get; set; } // Id и токены пользователей, которые могут делать запросы к антиплагиату. Один из пользователей — ulearn

		public DbSet<Submission> Submissions { get; set; } // Посылки: автор добавил такой-то код посылки (ссылка на таблицу Code) для задачи в такое время

		public DbSet<Code> Codes { get; set; } // Код посылки

		public DbSet<Snippet> Snippets { get; set; } // Hash (токена), SnippetType (только типы токенов или сами значения), TokensCount (количество токенов в сниппете). Сам набор токенов в снипете не хранится

		public DbSet<SnippetStatistics> SnippetsStatistics { get; set; } // Для задачи для сниппета количество уникальных авторов с этим сниппетом в этой задаче.

		public DbSet<SnippetOccurence> SnippetsOccurences { get; set; } // id сниппета, id посылки, местоположение сниппета в посылке

		// Для задачи количество посылок, мат ожидание и дисперсия распределения попарных расстояний между последними решениями 100 последних авторов.
		// Количество посылок здесь на момент последнего обновления статистик.
		// Статистики обновляются при увеличении количества в 2 раза, а потом каждую 1000. Идентичные решения не учитываются в статистике.
		public DbSet<TaskStatisticsParameters> TasksStatisticsParameters { get; set; }

		public DbSet<WorkQueueItem> WorkQueueItems { get; set; } // Очередь, которая может содержать что угодно. Используется для постановки задачи парсинга новой посылки в очередь, чтобы выгребать, когда есть время.

		public DbSet<TaskStatisticsSourceData> TaskStatisticsSourceData { get; set; } // Содержит данные, на основе которых считались TasksStatisticsParameters. Т.е. попарные веса решений 100 авторов. Полезно для построения графиков распределения весов.

		public DbSet<MostSimilarSubmission> MostSimilarSubmissions { get; set; } // Во время запроса для преподавателя информации о плагиате в эту таблицу записывается вес самого похожего решения. Полезно для принятия решения установке ручных suspicion levels (границ, когад показывается плашка).

		public DbSet<ManualSuspicionLevels> ManualSuspicionLevels { get; set; } // Вручную установленные границы похожести для показа плашек. (Админ курса может менять слева в верхнему угду страницы с подробностями обнаруженного списывания.)

		// Антиплагиат не показывает совпадения с посылками старше submissionInfluenceLimitInMonths.
		// Для этого ежедневно запускаемый UpdateOldSubmissionsFromStatisticsWorker обновляет таблицу SnippetsStatistics,
		// чтобы поле authorsCount содержало только авторов, которые отправляли свои посылки за последние submissionInfluenceLimitInMonths.
		// В OldSubmissionsInfluenceBorder хранится дата и время, с которой на данный момент учитываются посылки в SnippetsStatistics.
		// UpdateOldSubmissionsFromStatisticsWorker запускается ночью. Если пока обрабатываются сниппеты таска придет новое решение,
		// то результат SnippetsStatistics может разойтись на этого автора, пока снова кто-то не отправит решение с тем же сниппетом.
		// Но время работы над одним таском в среднем меньше минуты. Так что вероятность небольшая, если запускать ночью.
		public DbSet<OldSubmissionsInfluenceBorder> OldSubmissionsInfluenceBorder { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.HasDefaultSchema(DefaultSchema);

			modelBuilder.Entity<MostSimilarSubmission>()
				.HasOne(e => e.SimilarSubmission)
				.WithMany()
				.HasForeignKey(e => e.SimilarSubmissionId)
				.IsRequired()
				.OnDelete(DeleteBehavior.Restrict); // Introducing FOREIGN KEY constraint may cause cycles or multiple cascade paths, потому что две ссылки на одну таблицу

			modelBuilder.Entity<TaskStatisticsSourceData>()
				.HasOne(e => e.Submission2)
				.WithMany()
				.HasForeignKey(e => e.Submission2Id)
				.IsRequired()
				.OnDelete(DeleteBehavior.Restrict); // Introducing FOREIGN KEY constraint may cause cycles or multiple cascade paths, потому что две ссылки на одну таблицу

			modelBuilder.Entity<Client>()
				.HasIndex(c => c.Token)
				.IsUnique();
			modelBuilder.Entity<Client>()
				.HasIndex(c => new { c.Token, c.IsEnabled });

			var snippetOccurenceEntityBuilder = modelBuilder.Entity<SnippetOccurence>();
			snippetOccurenceEntityBuilder.HasIndex(c => new { c.SubmissionId, c.FirstTokenIndex });
			snippetOccurenceEntityBuilder.HasIndex(c => new { c.SubmissionId, c.SnippetId });
			snippetOccurenceEntityBuilder.HasIndex(c => new { c.SnippetId, c.SubmissionId });

			modelBuilder.Entity<Snippet>()
				.HasIndex(c => new { c.TokensCount, c.SnippetType, c.Hash })
				.IsUnique();

			modelBuilder.Entity<SnippetStatistics>()
				.HasIndex(c => new { c.SnippetId, c.TaskId, c.Language, c.ClientId })
				.IsUnique();

			var submissionEntityBuilder = modelBuilder.Entity<Submission>();
			submissionEntityBuilder.HasIndex(c => new { c.ClientId, c.TaskId, c.Language });
			submissionEntityBuilder.HasIndex(c => new { c.ClientId, c.TaskId, c.Language, c.AuthorId });
			submissionEntityBuilder.HasIndex(c => new { c.ClientId, c.TaskId, c.AddingTime, c.Language, c.AuthorId });
			submissionEntityBuilder.HasIndex(c => new { c.ClientId, c.ClientSubmissionId });
			submissionEntityBuilder.HasIndex(c => new { c.AddingTime });

			modelBuilder.Entity<WorkQueueItem>()
				.HasIndex(c => new { c.QueueId, c.TakeAfterTime })
				.IsUnique(false);

			modelBuilder.Entity<MostSimilarSubmission>()
				.HasIndex(c => new { c.Timestamp })
				.IsUnique(false);

			modelBuilder.Entity<TaskStatisticsParameters>()
				.HasKey(p => new { p.TaskId, p.Language });

			modelBuilder.Entity<ManualSuspicionLevels>()
				.HasKey(p => new { p.TaskId, p.Language });
		}

		public void MigrateToLatestVersion()
		{
			Database.Migrate();
		}

		/* We stands with performance issue on EF Core: https://github.com/aspnet/EntityFrameworkCore/issues/11680
			So we decided to disable AutoDetectChangesEnabled temporary for some queries */
		public void DisableAutoDetectChanges()
		{
			ChangeTracker.AutoDetectChangesEnabled = false;
		}

		public void EnableAutoDetectChanges()
		{
			ChangeTracker.AutoDetectChangesEnabled = true;
		}
	}
}