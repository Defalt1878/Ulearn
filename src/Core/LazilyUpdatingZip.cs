using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zip;
using Ionic.Zlib;
using Ulearn.Common;
using Ulearn.Common.Extensions;

namespace Ulearn.Core
{
	public class LazilyUpdatingZip
	{
		private readonly DirectoryInfo dir;
		private readonly string[] excludedDirs;
		private readonly Func<FileInfo, bool> needExcludeFile;
		private readonly Func<FileInfo, MemoryStream> getFileContent;
		private readonly FileInfo zipFile;
		private readonly IEnumerable<FileContent> filesToAdd;

		public LazilyUpdatingZip(DirectoryInfo dir, string[] excludedDirs, Func<FileInfo, bool> needExcludeFile, Func<FileInfo, MemoryStream> getFileContent, IEnumerable<FileContent> filesToAdd, FileInfo zipFile)
		{
			this.dir = dir;
			this.excludedDirs = excludedDirs;
			this.needExcludeFile = needExcludeFile;
			this.getFileContent = getFileContent;
			this.filesToAdd = filesToAdd;
			this.zipFile = zipFile;
		}

		public void UpdateZip()
		{
			if (IsActual())
				return;
			using var zip = new ZipFile(Encoding.UTF8);
			zip.CompressionLevel = CompressionLevel.BestSpeed;
			foreach (var f in EnumerateFiles())
			{
				var newContent = getFileContent(f);
				if (newContent == null)
					zip.AddFile(f.FullName, Path.GetDirectoryName(f.GetRelativePath(dir.FullName)));
				else
					zip.AddEntry(
						f.GetRelativePath(dir.FullName),
						_ =>
						{
							newContent.Position = 0;
							return newContent;
						}, (_, s) => s.Dispose()
					);
			}

			foreach (var fileToAdd in filesToAdd)
			{
				var directoriesList = GetDirectoriesList(fileToAdd.Path);
				if (!excludedDirs.Intersect(directoriesList).Any())
					zip.UpdateEntry(fileToAdd.Path, fileToAdd.Data);
			}

			zip.Save(zipFile.FullName);
		}

		public static IEnumerable<string> GetDirectoriesList(string filename)
		{
			var pathParts = filename.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			return pathParts.Take(pathParts.Length - 1);
		}

		private bool IsActual()
		{
			var lastWriteTime = EnumerateFiles().Max(f => f.LastWriteTimeUtc);
			return zipFile.Exists && zipFile.LastWriteTimeUtc > lastWriteTime;
		}

		public IEnumerable<FileInfo> EnumerateFiles()
		{
			return EnumerateFiles(dir);
		}

		private IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo aDir)
		{
			foreach (var f in aDir.GetFiles())
				if (!needExcludeFile(f))
					yield return f;
			var dirs = aDir.GetDirectories().Where(d => !excludedDirs.Contains(d.Name));
			foreach (var subDir in dirs)
			foreach (var f in EnumerateFiles(subDir))
				yield return f;
		}
	}
}