using System;
using System.IO;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Users;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types.Enums;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Helpers;
using Vostok.Logging.Abstractions;

namespace Ulearn.Web.Api.Utils.Courses
{
	public class MasterCourseManager : SlaveCourseManager, IMasterCourseManager
	{
		private readonly ExerciseStudentZipsCache exerciseStudentZipsCache;

		private readonly IServiceScopeFactory serviceScopeFactory;

		public MasterCourseManager(IServiceScopeFactory serviceScopeFactory, ExerciseStudentZipsCache exerciseStudentZipsCache)
			: base(serviceScopeFactory)
		{
			this.serviceScopeFactory = serviceScopeFactory;
			this.exerciseStudentZipsCache = exerciseStudentZipsCache;

			CourseStorageInstance.CourseChangedEvent += courseId =>
			{
				exerciseStudentZipsCache.DeleteCourseZips(courseId);
				ExerciseCheckerZipsCache.DeleteCourseZips(courseId);
			};
		}

		private static ILog Log => LogProvider.Get().ForContext(typeof(MasterCourseManager));

		// Не временные курсы не выкладываются на диск сразу, а публикуются в базу и UpdateCourses их обновляет на диске.
		public override async Task UpdateCoursesAsync()
		{
			using var scope = serviceScopeFactory.CreateScope();
			var coursesRepo = scope.ServiceProvider.GetService<ICoursesRepo>();
			var publishedCourseVersions = await coursesRepo.GetPublishedCourseVersions();
			foreach (var publishedCourseVersion in publishedCourseVersions)
				await UpdateCourseToVersionInDirectory(publishedCourseVersion, coursesRepo);
		}

		public async Task<FileInfo> GenerateOrFindStudentZip(string courseId, Slide slide)
		{
			return await exerciseStudentZipsCache.GenerateOrFindZip(courseId, slide, GetExtractedCourseDirectory(courseId).FullName);
		}

		private async Task UpdateCourseToVersionInDirectory(CourseVersion publishedCourseVersions, ICoursesRepo coursesRepo)
		{
			var courseId = publishedCourseVersions.CourseId;
			var publishedVersionToken = new CourseVersionToken(publishedCourseVersions.Id);
			if (BrokenVersions.ContainsKey(publishedVersionToken))
				return;
			var courseInMemory = CourseStorageInstance.FindCourse(courseId);
			if (courseInMemory is not null && courseInMemory.CourseVersionToken == publishedVersionToken)
				return;

			if (courseInMemory is null) // Проверяем, вдруг на диске актуальная версия
			{
				await UpdateCourseOrTempCourseToVersionFromDirectory(courseId, publishedVersionToken, TimeSpan.FromSeconds(0.1));
				courseInMemory = CourseStorageInstance.FindCourse(courseId);
				if (courseInMemory is not null && courseInMemory.CourseVersionToken == publishedVersionToken)
				{
					Log.Info($"Актуальная версия курса {courseId} {publishedVersionToken} просто загружена с диска");
					return;
				}
			}

			var courseFile = await coursesRepo.GetVersionFile(publishedCourseVersions.Id);
			await UpdateCourseInCommonDirectory(courseId, courseFile.File, publishedVersionToken, TimeSpan.FromSeconds(0.1));
		}

		private async Task UpdateCourseInCommonDirectory(string courseId, byte[] content, CourseVersionToken publishedVersionToken, TimeSpan? courseLockLimit)
		{
			using var courseDirectory = await ExtractCourseVersionToTemporaryDirectory(courseId, publishedVersionToken, content);
			var (course, exception) = LoadCourseFromDirectory(courseId, courseDirectory.DirectoryInfo);
			if (exception is null)
			{
				Log.Info($"Версия {publishedVersionToken} курса {courseId} успешно загружена из временной папки в память");
			}
			else
			{
				BrokenVersions.TryAdd(publishedVersionToken, true);
				var message = $"Не смог загрузить с диска в память курс {courseId} версии {publishedVersionToken}";
				Log.Error(exception, message);
				await PostCourseLoadingErrorToTelegram(courseId, exception);

				if (!CourseStorageInstance.HasCourse(courseId)) // Если не загружено вообще валидной версии, создаем версию с ошибкой
					await CreateErrorVersion(courseId, exception);
				return;
			}

			using var courseLock = await CourseLock.TryAcquireWriterLockAsync(courseId, courseLockLimit);
			if (!courseLock.IsLocked)
			{
				Log.Warn($"Не дождался разблокировки курса {courseId} за {courseLockLimit ?? TimeSpan.Zero}");
				return;
			}

			try
			{
				MoveCourse(courseDirectory.DirectoryInfo, GetExtractedCourseDirectory(courseId));
				CourseStorageUpdaterInstance.AddOrUpdateCourse(course);
			}
			catch (Exception ex)
			{
				Log.Error(ex, $"Не смог переместить курс {courseId} версия {publishedVersionToken} из временной папки в общую");
				await ErrorsBot.PostToChannelAsync($"Не смог переместить курс {courseId} версия {publishedVersionToken} из временной папки в общую:\n{ex.Message.EscapeMarkdown()}\n```{ex.StackTrace}```", ParseMode.Markdown);
			}

			Log.Info($"Версия {publishedVersionToken} курса {courseId} перемещена из временной папки в основную");
		}

		private async Task CreateErrorVersion(string courseId, Exception exception)
		{
			var fatalMessage = $"Выкладываю в курс {courseId} заглушку об ошибке. Срочно загрузите работающую версию";
			Log.Fatal(exception, fatalMessage);
			await ErrorsBot.PostToChannelAsync($"Fatal: {fatalMessage}:\n{exception.Message.EscapeMarkdown()}\n```{exception.StackTrace}```", ParseMode.Markdown);

			var errorVersion = Guid.NewGuid();
			using (var scope = serviceScopeFactory.CreateScope())
			{
				var usersRepo = scope.ServiceProvider.GetService<IUsersRepo>();
				var coursesRepo = scope.ServiceProvider.GetService<ICoursesRepo>();
				var ulearnBotUser = await usersRepo.GetUlearnBotUser();
				var courseTitle = (await coursesRepo.GetPublishedCourseVersion(courseId))?.CourseName;
				await CreateCourseVersionFromAnotherCourseVersion(courseId, errorVersion, courseTitle, ulearnBotUser.Id, CourseLoadingErrorCourseId);
			}

			Log.Warn($"Для курса {courseId} создана версия-заглушка {errorVersion} с информацией об ошибке на основе курса {CourseLoadingErrorCourseId}");
		}

		private void MoveCourse(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
		{
			// Не использую TempDirectory, потому что директория, в которую перемещаю, не должна существовать, иначе Move кинет ошибку.
			var tempDirectoryPath = Path.Combine(TempDirectory.TempDirectoryPath, Path.GetRandomFileName());
			try
			{
				destinationDirectory.EnsureExists();

				const int triesCount = 5;
				FuncUtils.TrySeveralTimes(() => Directory.Move(destinationDirectory.FullName, tempDirectoryPath), triesCount);

				try
				{
					FuncUtils.TrySeveralTimes(() => Directory.Move(sourceDirectory.FullName, destinationDirectory.FullName), triesCount);
				}
				catch (IOException)
				{
					/* In case of any file system's error rollback previous operation */
					FuncUtils.TrySeveralTimes(() => Directory.Move(tempDirectoryPath, destinationDirectory.FullName), triesCount);
					throw;
				}
			}
			finally
			{
				var tempDir = new DirectoryInfo(tempDirectoryPath);
				if (tempDir.Exists)
					tempDir.Delete(true);
			}
		}

		#region CreateAndUpdateTempCourses

		public string GetTempCourseId(string baseCourseId, string userId)
		{
			return $"{baseCourseId}_{userId}";
		}

		public async Task<TempCourse> CreateTempCourse(string baseCourseId, string userId)
		{
			var tempCourseId = GetTempCourseId(baseCourseId, userId);
			using var scope = serviceScopeFactory.CreateScope();
			var tempCoursesRepo = scope.ServiceProvider.GetService<ITempCoursesRepo>();
			var courseRolesRepo = scope.ServiceProvider.GetService<ICourseRolesRepo>();
			var coursesRepo = scope.ServiceProvider.GetService<ICoursesRepo>();

			var tmpCourseDbData = await tempCoursesRepo.Find(tempCourseId);
			if (tmpCourseDbData is not null)
			{
				Log.Warn($"Временный курс {tempCourseId} уже существует в базе");

				var course = CourseStorageInstance.FindCourse(tempCourseId);
				if (course is not null && course.CourseVersionToken == new CourseVersionToken(tmpCourseDbData.LoadingTime))
				{
					Log.Warn($"Временный курс {tempCourseId} версии {course.CourseVersionToken} уже загружен в память");
					return tmpCourseDbData;
				}
			}

			var loadingTime = DateTime.Now;
			var versionToken = new CourseVersionToken(loadingTime);
			var baseCourseVersionFile = await coursesRepo.GetPublishedVersionFile(baseCourseId);
			await UpdateCourseInCommonDirectory(tempCourseId, baseCourseVersionFile.File, versionToken, TimeSpan.FromSeconds(15));

			if (tmpCourseDbData is null)
			{
				tmpCourseDbData = await tempCoursesRepo.AddTempCourse(tempCourseId, userId, versionToken);
				await courseRolesRepo.ToggleRole(tempCourseId, userId, CourseRoleType.CourseAdmin, userId, "Создал временный курс");
				return tmpCourseDbData;
			}

			await tempCoursesRepo.UpdateTempCourseLoadingTime(tempCourseId, versionToken);
			return await tempCoursesRepo.Find(tempCourseId);
		}

		public async Task<(TempCourse Course, string Error)> UpdateTempCourseFromUserZipStream(string tempCourseId, Stream zipContent, bool isFullCourse)
		{
			using var scope = serviceScopeFactory.CreateScope();
			var loadingTime = DateTime.Now;
			var versionToken = new CourseVersionToken(loadingTime);
			var tempCoursesRepo = scope.ServiceProvider.GetService<ITempCoursesRepo>();

			using (await CourseLock.AcquireWriterLockAsync(tempCourseId))
			{
				var stagingTempCourseFile = GetStagingTempCourseFile(tempCourseId);
				await using (var fs = new FileStream(stagingTempCourseFile.FullName, FileMode.Create, FileAccess.Write))
				{
					await zipContent.CopyToAsync(fs);
				}

				var (course, exception) = await TryUpdateTempCourseOnDisk(tempCourseId, versionToken, isFullCourse);

				if (exception is not null)
				{
					var errorMessage = exception.Message;
					while (exception.InnerException is not null)
					{
						errorMessage += $"\n\n{exception.InnerException.Message}";
						exception = exception.InnerException;
					}

					await tempCoursesRepo.UpdateOrAddTempCourseError(tempCourseId, errorMessage);
					return (null, errorMessage);
				}

				CourseStorageUpdaterInstance.AddOrUpdateCourse(course);
				await tempCoursesRepo.MarkTempCourseAsNotErrored(tempCourseId);
				await tempCoursesRepo.UpdateTempCourseLoadingTime(tempCourseId, versionToken);
			}

			var tempCourse = await tempCoursesRepo.Find(tempCourseId);
			return (tempCourse, null);
		}

		private async Task<(Course Course, Exception Exception)> TryUpdateTempCourseOnDisk(string courseId, CourseVersionToken versionToken, bool isFull)
		{
			var courseDirectory = GetExtractedCourseDirectory(courseId);
			var stagingTempCourseFile = GetStagingTempCourseFile(courseId);

			var updater = new TempCourseOnDiskUpdater(courseDirectory, versionToken, stagingTempCourseFile, isFull);

			await updater.ApplyChanges();

			var (course, exception) = LoadCourseFromDirectory(courseId, courseDirectory);
			if (exception is not null)
			{
				Log.Warn(exception, $"Не смог загрузить с диска в память временный курс {courseId}. Откатываю.");

				await updater.Revert();

				Log.Warn(exception, $"Откатил временный курс {courseId}.");
			}

			return (course, exception);
		}

		#endregion
	}
}