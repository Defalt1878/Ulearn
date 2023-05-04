using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses;
using Vostok.Logging.Abstractions;

namespace Ulearn.Web.Api.Utils.Courses
{
	public class TempCourseOnDiskUpdater
	{
		private readonly DirectoryInfo courseDirectory;
		private readonly List<string> directoriesToDelete;
		private readonly List<string> filesToAdd;

		private readonly List<FileContent> filesToDelete;
		private readonly List<FileContent> filesToUpdateBeforeChanges;

		private readonly CourseVersionToken newVersionToken;
		private readonly CourseVersionToken versionTokenBeforeChanges;
		private readonly FileInfo zipWithChanges;

		public TempCourseOnDiskUpdater(DirectoryInfo courseDirectory, CourseVersionToken versionToken, FileInfo zipWithChanges, bool isFull)
		{
			this.courseDirectory = courseDirectory;
			this.zipWithChanges = zipWithChanges;
			newVersionToken = versionToken;
			versionTokenBeforeChanges = CourseVersionToken.Load(courseDirectory);
			CourseVersionToken.RemoveFile(courseDirectory);

			var pathPrefix = courseDirectory.FullName;
			var filesToDeleteRelativePaths = ParseDeletedTxt(zipWithChanges);
			var filesInDirectoriesToDelete = GetFilesInDirectoriesToDelete(filesToDeleteRelativePaths, pathPrefix);
			filesToDeleteRelativePaths.AddRange(filesInDirectoriesToDelete);
			var zip = ZipFile.Read(zipWithChanges.FullName, new ReadOptions { Encoding = Encoding.UTF8 });
			var filesToChangeRelativePaths = zip.Entries
				.Where(x => !x.IsDirectory)
				.Select(x => x.FileName)
				.Select(x => x.Replace('/', '\\'))
				.ToList();
			var courseFileRelativePaths = Directory
				.EnumerateFiles(courseDirectory.FullName, "*.*", SearchOption.AllDirectories)
				.Select(file => TrimPrefix(file, pathPrefix))
				.ToHashSet();

			if (isFull)
			{
				filesToDeleteRelativePaths.Clear();
				filesToDeleteRelativePaths.AddRange(courseFileRelativePaths);
			}

			var deletedFiles = filesToDeleteRelativePaths
				.Where(courseFileRelativePaths.Contains)
				.Select(relativePath => Path.Combine(pathPrefix, relativePath))
				.Select(path => new FileContent { Path = path, Data = File.ReadAllBytes(path) })
				.ToList();
			var deletedDirectories = GetDeletedDirs(filesToDeleteRelativePaths, pathPrefix);

			filesToUpdateBeforeChanges = filesToChangeRelativePaths
				.Where(courseFileRelativePaths.Contains)
				.Select(path => Path.Combine(pathPrefix, path))
				.Select(path => new FileContent { Path = path, Data = File.ReadAllBytes(path) })
				.ToList();
			filesToAdd = filesToChangeRelativePaths
				.Where(file => !courseFileRelativePaths.Contains(file))
				.Select(path => Path.Combine(pathPrefix, path))
				.ToList();
			filesToDelete = deletedFiles;
			directoriesToDelete = deletedDirectories;
		}

		private static ILog Log => LogProvider.Get().ForContext(typeof(TempCourseOnDiskUpdater));

		public async Task ApplyChanges()
		{
			DeleteFiles(filesToDelete, directoriesToDelete);
			DeleteEmptySubdirectories(courseDirectory.FullName);
			UnzipWithOverwrite(zipWithChanges, courseDirectory);
			await newVersionToken.Save(courseDirectory);
		}

		public async Task Revert()
		{
			filesToDelete.ForEach(file => new FileInfo(file.Path).Directory!.Create());

			static void WriteContent(FileContent fileContent)
			{
				var fInfo = new FileInfo(fileContent.Path);
				if (fInfo.Exists && fInfo.Attributes.HasFlag(FileAttributes.Hidden))
					fInfo.Attributes &= ~FileAttributes.Hidden; // WriteAllBytes кидает ошибку при записи в скрытый файл
				File.WriteAllBytes(fileContent.Path, fileContent.Data);
			}

			filesToUpdateBeforeChanges.ForEach(WriteContent);
			filesToDelete.ForEach(WriteContent);
			filesToAdd.ForEach(File.Delete);
			await versionTokenBeforeChanges.Save(courseDirectory);
		}

		private static void UnzipWithOverwrite(FileSystemInfo zipFile, DirectoryInfo unpackDirectory)
		{
			using var zip = ZipFile.Read(zipFile.FullName, new ReadOptions { Encoding = Encoding.UTF8 });
			zip.ExtractAll(unpackDirectory.FullName, ExtractExistingFileAction.OverwriteSilently);
			foreach (var f in unpackDirectory.GetFiles("*", SearchOption.AllDirectories).Cast<FileSystemInfo>().Concat(unpackDirectory.GetDirectories("*", SearchOption.AllDirectories)))
				f.Attributes &= ~FileAttributes.ReadOnly;
			var deletedTxt = unpackDirectory.GetFile("deleted.txt");
			if (deletedTxt.Exists)
				deletedTxt.Delete();
			Log.Info($"Архив {zipFile.FullName} распакован");
		}

		private static List<string> ParseDeletedTxt(FileSystemInfo stagingTempCourseFile)
		{
			var filesToDeleteLocal = new List<string>();
			using (var zip = ZipFile.Read(stagingTempCourseFile.FullName))
			{
				var e = zip["deleted.txt"];
				if (e is null)
					return new List<string>();
				var r = e.OpenReader();
				using var sr = new StreamReader(r);
				while (!sr.EndOfStream)
				{
					var line = sr.ReadLine();
					if (!string.IsNullOrEmpty(line))
						filesToDeleteLocal.Add(line);
				}
			}

			return filesToDeleteLocal
				.Select(x =>
				{
					if (x.StartsWith('\\') || x.StartsWith('/'))
						return x[1..];
					return x;
				}).ToList();
		}

		private static void DeleteFiles(List<FileContent> filesToDelete, List<string> directoriesToDelete)
		{
			filesToDelete.ForEach(file => File.Delete(file.Path));
			directoriesToDelete.ForEach(DeleteNotEmptyDirectory);
		}

		private static void DeleteNotEmptyDirectory(string dirPath)
		{
			var files = Directory.GetFiles(dirPath);
			var dirs = Directory.GetDirectories(dirPath);

			foreach (var file in files)
			{
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
			}

			foreach (var dir in dirs)
				DeleteNotEmptyDirectory(dir);

			Directory.Delete(dirPath, false);
		}

		private static void DeleteEmptySubdirectories(string startLocation)
		{
			foreach (var directory in Directory.GetDirectories(startLocation))
			{
				DeleteEmptySubdirectories(directory);
				if (Directory.GetFiles(directory).Length == 0 &&
					Directory.GetDirectories(directory).Length == 0)
					Directory.Delete(directory, false);
			}
		}

		private static IEnumerable<string> GetFilesInDirectoriesToDelete(IEnumerable<string> filesToDeleteRelativePaths, string pathPrefix)
		{
			return filesToDeleteRelativePaths
				.Select(path => Path.Combine(pathPrefix, path))
				.Where(Directory.Exists)
				.SelectMany(dir => Directory
					.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
				.Select(path => TrimPrefix(path, pathPrefix));
		}

		private static string TrimPrefix(string text, string prefix)
		{
			return text[(text.IndexOf(prefix) + prefix.Length + 1)..];
		}

		private static List<string> GetDeletedDirs(IEnumerable<string> filesToDeleteRelativePaths, string pathPrefix)
		{
			return filesToDeleteRelativePaths.Select(path => Path.Combine(pathPrefix, path))
				.Where(path => Directory.Exists(path) &&
								path.StartsWith(pathPrefix) &&
								!path.Contains(".."))
				.ToList();
		}
	}
}