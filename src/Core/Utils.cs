using System;
using System.IO;
using System.Reflection;

namespace Ulearn.Core
{
	public static class Utils
	{
		public static string WebApplicationPhysicalPath { get; set; }

		public static string NewNormalizedGuid()
		{
			return Guid.NewGuid().ToString("D");
		}

		public static string GetNormalizedGuid(string guid)
		{
			return Guid.Parse(guid).ToString("D");
		}

		public static string GetNormalizedGuid(this Guid guid)
		{
			return guid.ToString("D");
		}

		public static string GetRootDirectory()
		{
			return AppDomain.CurrentDomain.BaseDirectory;
		}

		public static void DeleteFileIfExists(string file)
		{
			if (File.Exists(file))
				File.Delete(file);
		}

		public static void DeleteDirectoryIfExists(string directory)
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, true);
		}

		public static string GetPass()
		{
			var password = "";
			ConsoleKeyInfo info;
			while ((info = Console.ReadKey(true)).Key != ConsoleKey.Enter)
				if (info.Key != ConsoleKey.Backspace)
					password += info.KeyChar;
				else if (!string.IsNullOrEmpty(password))
					password = password[..^1];
			return password;
		}

		public static void DirectoryCopy(string source, string dest, bool recursive)
		{
			var dir = new DirectoryInfo(source);
			var dirs = dir.GetDirectories();

			if (!dir.Exists)
				throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + source);

			if (!Directory.Exists(dest))
				Directory.CreateDirectory(dest);

			foreach (var file in dir.GetFiles())
			{
				var tempPath = Path.Combine(dest, file.Name);
				file.CopyTo(tempPath, true);
			}

			if (recursive)
				foreach (var subDir in dirs)
				{
					var tempPath = Path.Combine(dest, subDir.Name);
					DirectoryCopy(subDir.FullName, tempPath, true);
				}
		}

		public static bool IsInside(this DirectoryInfo baseDir, FileSystemInfo subElement)
		{
			var nBaseDir = NormalizePath(baseDir.FullName);
			var nSubDir = NormalizePath(subElement.FullName);
			return nSubDir.StartsWith(nBaseDir);
		}

		private static string NormalizePath(string path)
		{
			return Path.GetFullPath(new Uri(path).LocalPath)
				.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
				.ToUpperInvariant();
		}

		public static string GetAppPath()
		{
			if (WebApplicationPhysicalPath != null)
				return WebApplicationPhysicalPath;
			return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		}
	}
}