using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Configuration;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides;
using Vostok.Logging.Abstractions;

namespace Ulearn.Core.Helpers
{
	public class ExerciseStudentZipsCache
	{
		private static ILog Log => LogProvider.Get().ForContext(typeof(ExerciseStudentZipsCache));

		private readonly DirectoryInfo cacheDirectory;
		private readonly ExerciseStudentZipBuilder builder;

		public ExerciseStudentZipsCache(IOptions<UlearnConfiguration> options)
		{
			var exerciseStudentZipsDirectory = options.Value.ExerciseStudentZipsDirectory;
			cacheDirectory = GetCacheDirectory(exerciseStudentZipsDirectory);
			cacheDirectory.EnsureExists();
			builder = new ExerciseStudentZipBuilder();
		}

		private static DirectoryInfo GetCacheDirectory(string exerciseStudentZipsDirectory)
		{
			if (!string.IsNullOrEmpty(exerciseStudentZipsDirectory))
				return new DirectoryInfo(exerciseStudentZipsDirectory);

			return CourseManager.CoursesDirectory.GetSubdirectory("ExerciseStudentZips");
		}

		public async Task<FileInfo> GenerateOrFindZip(string courseId, Slide slide, string courseDirectory)
		{
			var cacheCourseDirectory = cacheDirectory.GetSubdirectory(courseId);
			var zipFile = cacheCourseDirectory.GetFile($"{slide.Id}.zip");
			if (!zipFile.Exists)
			{
				cacheCourseDirectory.EnsureExists();
				Log.Info($"Собираю zip-архив с упражнением: курс {courseId}, слайд «{slide.Title}» ({slide.Id}), файл {zipFile.FullName}");
				using (await CourseLock.AcquireReaderLockAsync(courseId))
				{
					builder.BuildStudentZip(slide, zipFile, courseDirectory);
				}
			}

			return zipFile;
		}

		public void DeleteCourseZips(string courseId)
		{
			Log.Info($"Очищаю папку со сгенерированными zip-архивами для упражнений из курса {courseId}");

			var courseDirectory = cacheDirectory.GetSubdirectory(courseId);
			courseDirectory.EnsureExists();

			FuncUtils.TrySeveralTimes(() => courseDirectory.ClearDirectory(), 3);
		}
	}
}