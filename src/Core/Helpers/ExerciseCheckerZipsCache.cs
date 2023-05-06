using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Ionic.Zip;
using JetBrains.Annotations;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Configuration;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Exercises.Blocks;
using Vostok.Logging.Abstractions;

namespace Ulearn.Core.Helpers
{
	public static class ExerciseCheckerZipsCache
	{
		private static readonly bool isDisabled;
		private static readonly DirectoryInfo cacheDirectory;
		private static readonly UlearnConfiguration configuration;
		private static readonly HashSet<string> coursesLockedForDelete;
		private static readonly ConcurrentDictionary<string, int> courseToFileLocksNumber;

		static ExerciseCheckerZipsCache()
		{
			configuration = ApplicationConfiguration.Read<UlearnConfiguration>();
			isDisabled = configuration.ExerciseCheckerZipsCacheDisabled;
			if (isDisabled)
				return;
			cacheDirectory = GetCacheDirectory();
			coursesLockedForDelete = new HashSet<string>();
			courseToFileLocksNumber = new ConcurrentDictionary<string, int>();
			cacheDirectory.EnsureExists();
		}

		private static ILog Log => LogProvider.Get().ForContext(typeof(ExerciseCheckerZipsCache));

		private static DirectoryInfo GetCacheDirectory()
		{
			var directory = configuration.ExerciseCheckerZipsDirectory;
			if (!string.IsNullOrEmpty(directory))
				return new DirectoryInfo(directory);

			return CourseManager.CoursesDirectory.GetSubdirectory("ExerciseCheckerZips");
		}

		public static MemoryStream GetZip(IExerciseCheckerZipBuilder zipBuilder, string userCodeFilePathRelativeToUnit, byte[] userCodeFileContent, string courseDirectory)
		{
			var courseId = zipBuilder.CourseId;
			var slide = zipBuilder.Slide;
			MemoryStream ms = null;
			if (isDisabled || coursesLockedForDelete.Contains(courseId) || courseId == null || slide == null)
				ms = zipBuilder.GetZipForChecker();
			else
				try
				{
					WithLock(courseId, () =>
					{
						var cacheCourseDirectory = cacheDirectory.GetSubdirectory(courseId);
						cacheCourseDirectory.EnsureExists();
						var zipFile = cacheCourseDirectory.GetFile($"{slide.Id}.zip");
						if (!zipFile.Exists)
						{
							ms = zipBuilder.GetZipForChecker();
							SaveFileOnDisk(zipFile, ms);
						}
						else
						{
							ms = StaticRecyclableMemoryStreamManager.Manager.GetStream();
							using var stream = zipFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
							stream.CopyTo(ms);
						}
					});
				}
				catch (Exception ex)
				{
					//log.Warn($"Exception in write or read checker zip from cache courseId {courseId} slideId {slide.Id}", ex);
					ms = zipBuilder.GetZipForChecker();
				}

			using (ms)
			{
				return AddUserCodeToZip(ms, userCodeFilePathRelativeToUnit, userCodeFileContent, courseId, slide);
			}
		}

		private static void SaveFileOnDisk(FileInfo zipFile, MemoryStream ms)
		{
			try
			{
				using var fileStream = zipFile.Open(FileMode.CreateNew, FileAccess.Write, FileShare.None);
				fileStream.Write(ms.ToArray(), 0, (int)ms.Length);
			}
			catch (Exception ex)
			{
				Log.Warn(ex, $"Exception in SaveFileOnDisk courseId {zipFile.FullName}");
			}
		}

		private static void WithLock(string courseId, Action action)
		{
			courseToFileLocksNumber.AddOrUpdate(courseId, 1, (_, value) => ++value);
			try
			{
				action();
			}
			finally
			{
				while (true)
				{
					var value = courseToFileLocksNumber[courseId];
					if (courseToFileLocksNumber.TryUpdate(courseId, value - 1, value))
						break;
				}
			}
		}

		private static MemoryStream AddUserCodeToZip(MemoryStream inputStream, string userCodeFilePath, byte[] userCodeFileContent, [CanBeNull] string courseId, [CanBeNull] Slide slide)
		{
			var sw = Stopwatch.StartNew();
			var resultStream = StaticRecyclableMemoryStreamManager.Manager.GetStream();
			inputStream.Position = 0;
			using (var zip = ZipFile.Read(inputStream))
			{
				if (zip.ContainsEntry(userCodeFilePath))
					zip.UpdateEntry(userCodeFilePath, userCodeFileContent);
				else
					zip.AddEntry(userCodeFilePath, userCodeFileContent);
				zip.Save(resultStream);
				resultStream.Position = 0;
			}

			Log.Info($"Добавил код студента в zip-архив с упражнением: курс {courseId}, слайд «{slide?.Title}» ({slide?.Id}) elapsed {sw.ElapsedMilliseconds} ms");
			return resultStream;
		}

		public static void DeleteCourseZips(string courseId)
		{
			if (isDisabled)
				return;

			Log.Info($"Очищаю папку со сгенерированными zip-архивами для упражнений из курса {courseId}");

			var courseDirectory = cacheDirectory.GetSubdirectory(courseId);
			courseDirectory.EnsureExists();

			coursesLockedForDelete.Add(courseId);
			try
			{
				while (courseToFileLocksNumber.GetValueOrDefault(courseId, 0) > 0)
					Thread.Sleep(10);

				FuncUtils.TrySeveralTimes(() => courseDirectory.ClearDirectory(), 3);
			}
			finally
			{
				coursesLockedForDelete.Remove(courseId);
			}
		}
	}
}