using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Ionic.Zip;
using JetBrains.Annotations;

namespace Ulearn.Common
{
	public static class ZipUtils
	{
		static ZipUtils()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}

		public static Encoding Cp866 => Encoding.GetEncoding(866);

		public static MemoryStream CreateZipFromDirectory([NotNull] List<string> directoriesToInclude, [CanBeNull] List<string> excludeCriterias,
			[CanBeNull] IEnumerable<FileContent> filesToUpdateOrCreate)
		{
			excludeCriterias ??= new List<string>();
			var excludeRegexps = GetExcludeRegexps(excludeCriterias).ToList();
			using var zip = new ZipFile(Encoding.UTF8);
			foreach (var pathToDirectory in directoriesToInclude)
			{
				var allFiles = Directory.GetFiles(pathToDirectory, "*.*", SearchOption.AllDirectories)
					.Select(p => p.Replace(pathToDirectory, "").TrimStart('/').TrimStart('\\')).ToList();
				foreach (var filePath in allFiles)
				{
					var isExcluded = IsExcluded(filePath, excludeRegexps);
					if (isExcluded)
						continue;
					var fullPath = Path.Combine(pathToDirectory, filePath);
					var bytes = File.ReadAllBytes(fullPath);
					zip.UpdateEntry(filePath, bytes);
				}
			}

			foreach (var zipUpdateData in filesToUpdateOrCreate.EmptyIfNull())
			{
				var isExcluded = IsExcluded(zipUpdateData.Path, excludeRegexps);
				if (isExcluded)
					continue;
				zip.UpdateEntry(zipUpdateData.Path, zipUpdateData.Data);
			}

			var ms = StaticRecyclableMemoryStreamManager.Manager.GetStream();
			zip.Save(ms);
			ms.Position = 0;
			return ms;
		}

		private static bool IsExcluded(string filePath, List<Regex> excludeRegexps)
		{
			filePath = filePath.Replace('\\', '/');
			return excludeRegexps.Any(regex => regex.IsMatch(filePath));
		}

		// Пути не имеют ведущего /
		// Если / в конце, то папка, иначе файл
		// Если / в начале, то ищем от корневой директории, иначе путь может начинаться в поддиректории
		// * - 0 или более любых символов, кроме /
		// ? - 0 или 1 любой символ, кроме /
		private static IEnumerable<Regex> GetExcludeRegexps(List<string> excludeCriterias)
		{
			foreach (var excludeCriteria in excludeCriterias)
			{
				var criterion = excludeCriteria.Trim();
				criterion = criterion.Replace('\\', '/');
				if (criterion.Length == 0)
					continue;
				var isDirectory = false;
				if (criterion.Last() == '/')
				{
					isDirectory = true;
					criterion = criterion[..^1];
				}

				var isPathFromRoot = criterion.StartsWith("/");
				if (isPathFromRoot)
					criterion = criterion.Remove(0, 1);
				var regexpSb = new StringBuilder();
				regexpSb.Append(isPathFromRoot ? "^" : "(^|/)");
				const string specChars = "[]^$.|+(){}";
				criterion = specChars.Aggregate(criterion, (current, specChar) => current.Replace(specChar.ToString(), "\\" + specChar));
				criterion = criterion
					.Replace("*", "[^/]*")
					.Replace("?", "[^/]?");
				regexpSb.Append(criterion);
				regexpSb.Append(isDirectory ? '/' : '$');
				var regex = new Regex(regexpSb.ToString(), RegexOptions.Compiled);
				yield return regex;
			}
		}

		public static void UnpackZip(byte[] data, string pathToExtractDir)
		{
			using var ms = new MemoryStream(data);
			using var zip = ZipFile.Read(ms, new ReadOptions { Encoding = Cp866 });
			foreach (var file in zip)
				try
				{
					file.Extract(pathToExtractDir, ExtractExistingFileAction.OverwriteSilently);
				}
				catch (Exception e)
				{
					throw new IOException("File " + file.FileName, e);
				}
		}

		public static Stream GetZipWithFileWithNameInRoot(string inputZipFileName, string fileNameInRoot)
		{
			using var zip = ZipFile.Read(inputZipFileName, new ReadOptions { Encoding = Cp866 });
			var rootFiles = zip.SelectEntries(fileNameInRoot);
			if (rootFiles.Count == 0 || rootFiles.Any(x => x.FileName.Equals(fileNameInRoot, StringComparison.OrdinalIgnoreCase)))
				return new FileStream(inputZipFileName, FileMode.Open, FileAccess.Read);

			var rootFile = rootFiles.First();
			var rootFileDirectory = Path.GetDirectoryName(rootFile.FileName);
			var entries = zip.SelectEntries(rootFileDirectory + "/*").Where(e => !e.IsDirectory);
			using var newZip = new ZipFile(Encoding.UTF8)
			{
				CompressionLevel = zip.CompressionLevel,
				CompressionMethod = zip.CompressionMethod
			};
			var toDispose = new List<MemoryStream>();
			try
			{
				foreach (var entry in entries)
				{
					var newName = entry.FileName.Remove(0, rootFileDirectory!.Length + 1);
					var ms = StaticRecyclableMemoryStreamManager.Manager.GetStream();
					toDispose.Add(ms);
					entry.Extract(ms);
					ms.Position = 0;
					newZip.AddEntry(newName, ms);
				}

				var result = StaticRecyclableMemoryStreamManager.Manager.GetStream();
				newZip.Save(result);
				result.Position = 0;
				return result;
			}
			finally
			{
				toDispose.ForEach(s => s.Dispose());
			}
		}
	}
}