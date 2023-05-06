using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Configuration;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Units;
using Ulearn.Core.Helpers;
using Ulearn.Core.Telegram;
using Vostok.Logging.Abstractions;

namespace Ulearn.Core.Courses.Manager
{
	public abstract class CourseManager
	{
		public const string ExampleCourseId = "Help";
		public const string CourseLoadingErrorCourseId = "course-loading-error";

		public static readonly DirectoryInfo CoursesDirectory;
		public static readonly DirectoryInfo ExtractedCoursesDirectory;
		private static readonly DirectoryInfo tempCourseStagingDirectory;

		public static readonly char[] InvalidForCourseIdCharacters = new[] { '&', CourseLoader.CourseIdDelimiter }.Concat(Path.GetInvalidFileNameChars()).Concat(Path.GetInvalidPathChars()).Distinct().ToArray();

		protected static readonly ErrorsBot ErrorsBot = new();

		protected static readonly ConcurrentDictionary<CourseVersionToken, bool> BrokenVersions = new();

		private static readonly CourseLoader loader = new(new UnitLoader(new XmlSlideLoader()));
		private static readonly CourseStorage courseStorage = new();

		static CourseManager()
		{
			CoursesDirectory = GetCoursesDirectory();
			ExtractedCoursesDirectory = CoursesDirectory.GetSubdirectory("Courses");
			tempCourseStagingDirectory = CoursesDirectory.GetSubdirectory("TempCourseStaging");
			EnsureDirectoriesExist();
		}

		public static ICourseStorage CourseStorageInstance => courseStorage;
		public static ICourseStorageUpdater CourseStorageUpdaterInstance => courseStorage;

		private static ILog log => LogProvider.Get().ForContext(typeof(CourseManager));

		public static void EnsureDirectoriesExist()
		{
			CoursesDirectory.EnsureExists();
			ExtractedCoursesDirectory.EnsureExists();
			tempCourseStagingDirectory.EnsureExists();
		}

		public bool IsCourseIdAllowed(string courseId)
		{
			return !courseId.Any(InvalidForCourseIdCharacters.Contains);
		}

		private static DirectoryInfo GetCoursesDirectory()
		{
			var coursesDirectory = ApplicationConfiguration.Read<UlearnConfiguration>().CoursesDirectory ?? "";
			if (!Path.IsPathRooted(coursesDirectory))
				coursesDirectory = Path.Combine(Utils.GetAppPath(), coursesDirectory);

			return new DirectoryInfo(coursesDirectory);
		}

		protected static DirectoryInfo GetExtractedCourseDirectory(string courseId)
		{
			return ExtractedCoursesDirectory.GetSubdirectory(courseId);
		}

		protected static FileInfo GetStagingTempCourseFile(string courseId)
		{
			return tempCourseStagingDirectory.GetFile(courseId + ".zip");
		}

		protected static async Task PostCourseLoadingErrorToTelegram(string courseId, Exception ex)
		{
			await ErrorsBot.PostToChannelAsync($"Не смог загрузить курс из папки {GetExtractedCourseDirectory(courseId)}:\n{ex.Message.EscapeMarkdown()}\n```{ex.StackTrace}```", ParseMode.Markdown);
		}

		#region WorkWithCourseInTemporaryDirectory

		public async Task<TempDirectory> ExtractCourseVersionToTemporaryDirectory(string courseId, CourseVersionToken versionToken, byte[] zipContent)
		{
			var tempDirectory = CreateCourseTempDirectory(courseId, versionToken);
			ZipUtils.UnpackZip(zipContent, tempDirectory.DirectoryInfo.FullName);
			await versionToken.Save(tempDirectory.DirectoryInfo);
			return tempDirectory;
		}

		private static readonly Random randomGenerator = new();

		private TempDirectory CreateCourseTempDirectory(string courseId, CourseVersionToken versionToken)
		{
			// @ — разделитель courseId и остального. используется CourseLoader
			var directoryName = $"{courseId}{CourseLoader.CourseIdDelimiter}{versionToken}_{DateTime.Now.ToSortable()}_{randomGenerator.Next()}";
			return new TempDirectory(directoryName);
		}

		public TempFile SaveVersionZipToTemporaryDirectory(string courseId, CourseVersionToken versionToken, Stream stream)
		{
			var fileName = $"{courseId}{CourseLoader.CourseIdDelimiter}{versionToken}_{DateTime.Now.ToSortable()}_{randomGenerator.Next()}.zip";
			return new TempFile(fileName, stream);
		}

		public (Course Course, Exception Exception) LoadCourseFromDirectory(string courseId, DirectoryInfo extractedCourseDirectory)
		{
			try
			{
				var course = loader.Load(extractedCourseDirectory, courseId);
				return (course, null);
			}
			catch (Exception e)
			{
				log.Warn(e, $"Upload course from temp directory exception '{courseId}'");
				return (null, e);
			}
		}

		#endregion

		#region UpdateInMemoryCourseFromCommonDirectory

		protected async Task UpdateCourseOrTempCourseToVersionFromDirectory(string courseId, CourseVersionToken publishedVersionToken, TimeSpan? courseLockLimit)
		{
			if (BrokenVersions.ContainsKey(publishedVersionToken))
				return;
			var courseInMemory = CourseStorageInstance.FindCourse(courseId);
			if (courseInMemory != null && courseInMemory.CourseVersionToken == publishedVersionToken)
				return;
			try
			{
				await UpdateCourseFromDirectoryIfRightVersion(courseId, publishedVersionToken, courseLockLimit);
			}
			catch (Exception ex)
			{
				BrokenVersions.TryAdd(publishedVersionToken, true);
				var message = $"Не смог загрузить с диска в память курс {courseId} версии {publishedVersionToken}";
				if (publishedVersionToken.IsTempCourse())
				{
					log.Warn(ex, message);
				}
				else
				{
					log.Error(ex, message);
					await PostCourseLoadingErrorToTelegram(courseId, ex);
				}
			}
		}

		private async Task UpdateCourseFromDirectoryIfRightVersion(string courseId, CourseVersionToken publishedVersionToken, TimeSpan? courseLockLimit)
		{
			var courseDirectory = GetExtractedCourseDirectory(courseId);
			if (!courseDirectory.Exists)
				return;
			using var courseLock = await CourseLock.TryAcquireReaderLockAsync(courseId, courseLockLimit);
			if (!courseLock.IsLocked)
			{
				log.Warn($"Не дождался разблокировки курса {courseId} за {courseLockLimit ?? TimeSpan.Zero}");
				return;
			}

			var courseVersionToken = CourseVersionToken.Load(courseDirectory);
			if (courseVersionToken != publishedVersionToken)
				return;
			if (!courseDirectory.Exists)
				return;
			var course = loader.Load(courseDirectory, courseId);
			CourseStorageUpdaterInstance.AddOrUpdateCourse(course);
		}

		#endregion
	}
}