using System;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Ulearn.Core.Courses.Manager;
using Vostok.Logging.Abstractions;

namespace Ulearn.Web.Api.Utils.Courses
{
	public class TempCourseRemover
	{
		private readonly IMasterCourseManager courseManager;
		private readonly ICourseStorageUpdater courseStorageUpdater;

		private readonly UlearnDb db;

		public TempCourseRemover(UlearnDb db, IMasterCourseManager courseManager, ICourseStorageUpdater courseStorageUpdater)
		{
			this.db = db;
			this.courseManager = courseManager;
			this.courseStorageUpdater = courseStorageUpdater;
		}

		private static ILog Log => LogProvider.Get().ForContext(typeof(TempCourseRemover));

		// Скрипт получения списка таблиц, где есть колонка CourseId
		/*
		select t.table_schema,
				t.table_name
		from information_schema.tables t
		inner join information_schema.columns c on c.table_name = t.table_name 
										and c.table_schema = t.table_schema
		where c.column_name = 'CourseId'
			and t.table_schema not in ('information_schema', 'pg_catalog')
			and t.table_type = 'BASE TABLE'
		order by t.table_name;
		*/

		// TODO не протестировано, что удаление работает в таком порядке.
		public async Task RemoveTempCourseWithAllData(string baseCourseId, string userId)
		{
			var tempCourseId = courseManager.GetTempCourseId(baseCourseId, userId);
			Log.Info($"Удаляю временный курс {tempCourseId} со всеми данными");

			await using (var transaction = await db.Database.BeginTransactionAsync())
			{
				db.AcceptedSolutionsPromotes.RemoveRange(db.AcceptedSolutionsPromotes.Where(e => e.CourseId == tempCourseId));
				db.AdditionalScores.RemoveRange(db.AdditionalScores.Where(e => e.CourseId == tempCourseId));
				db.AutomaticExerciseCheckings.RemoveRange(db.AutomaticExerciseCheckings.Where(e => e.CourseId == tempCourseId));
				db.AutomaticQuizCheckings.RemoveRange(db.AutomaticQuizCheckings.Where(e => e.CourseId == tempCourseId));
				db.CertificateTemplates.RemoveRange(db.CertificateTemplates.Where(e => e.CourseId == tempCourseId));
				db.Comments.RemoveRange(db.Comments.Where(e => e.CourseId == tempCourseId));
				db.CommentsPolicies.RemoveRange(db.CommentsPolicies.Where(e => e.CourseId == tempCourseId));
				db.CourseAccesses.RemoveRange(db.CourseAccesses.Where(e => e.CourseId == tempCourseId));
				db.CourseGitRepos.RemoveRange(db.CourseGitRepos.Where(e => e.CourseId == tempCourseId));
				db.CourseVersionFiles.RemoveRange(db.CourseVersionFiles.Where(e => e.CourseId == tempCourseId));
				db.CourseVersions.RemoveRange(db.CourseVersions.Where(e => e.CourseId == tempCourseId));
				db.ExerciseCodeReviews.RemoveRange(db.ExerciseCodeReviews.Where(e => e.CourseId == tempCourseId));
				// db.GraderClients.RemoveRange(db.GraderClients.Where(e => e.CourseId == tempCourseId));
				db.Groups.RemoveRange(db.Groups.Where(e => e.CourseId == tempCourseId));
				db.LastVisits.RemoveRange(db.LastVisits.Where(e => e.CourseId == tempCourseId));
				// Likes
				db.SolutionLikes.RemoveRange(db.SolutionLikes.Where(e => e.CourseId == tempCourseId));
				db.LtiRequests.RemoveRange(db.LtiRequests.Where(e => e.CourseId == tempCourseId));
				db.ManualExerciseCheckings.RemoveRange(db.ManualExerciseCheckings.Where(e => e.CourseId == tempCourseId));
				db.ManualQuizCheckings.RemoveRange(db.ManualQuizCheckings.Where(e => e.CourseId == tempCourseId));
				db.NotificationTransportSettings.RemoveRange(db.NotificationTransportSettings.Where(e => e.CourseId == tempCourseId));
				db.Notifications.RemoveRange(db.Notifications.Where(e => e.CourseId == tempCourseId));
				db.Hints.RemoveRange(db.Hints.Where(e => e.CourseId == tempCourseId));
				//db.SlideRates.RemoveRange(db.SlideRates.Where(e => e.CourseId == tempCourseId));
				db.TempCourseErrors.RemoveRange(db.TempCourseErrors.Where(e => e.CourseId == tempCourseId));
				db.TempCourses.RemoveRange(db.TempCourses.Where(e => e.CourseId == tempCourseId));
				db.UnitAppearances.RemoveRange(db.UnitAppearances.Where(e => e.CourseId == tempCourseId));
				db.UserExerciseSubmissions.RemoveRange(db.UserExerciseSubmissions.Where(e => e.CourseId == tempCourseId));
				db.UserFlashcardsUnlocking.RemoveRange(db.UserFlashcardsUnlocking.Where(e => e.CourseId == tempCourseId));
				db.UserFlashcardsVisits.RemoveRange(db.UserFlashcardsVisits.Where(e => e.CourseId == tempCourseId));
				//db.UserQuestions.RemoveRange(db.UserQuestions.Where(e => e.CourseId == tempCourseId));
				db.UserQuizSubmissions.RemoveRange(db.UserQuizSubmissions.Where(e => e.CourseId == tempCourseId));
				db.Visits.RemoveRange(db.Visits.Where(e => e.CourseId == tempCourseId));
				// UserRoles
				db.CourseRoles.RemoveRange(db.CourseRoles.Where(e => e.CourseId == tempCourseId));
				await db.SaveChangesAsync();
				await transaction.CommitAsync();
			}

			Log.Info($"Удалил данные временного курса {tempCourseId} из базы, жду 30 сек перед чисткой CourseStorage");

			await Task.Delay(TimeSpan.FromSeconds(30)); // Жду, чтобы завершилось возможно идущее обновление временных курсов.

			courseStorageUpdater.TryRemoveCourse(tempCourseId);

			db.TempCourseErrors.RemoveRange(db.TempCourseErrors.Where(e => e.CourseId == tempCourseId));
			db.TempCourses.RemoveRange(db.TempCourses.Where(e => e.CourseId == tempCourseId));

			Log.Info($"Временный курс {tempCourseId} удален со всеми данными");
		}
	}
}