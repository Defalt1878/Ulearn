using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ulearn.Common.Extensions;

public static class FileSystemExtensions
{
	public static bool HasSubdirectory(this DirectoryInfo root, string name)
	{
		return root.GetSubdirectory(name).Exists;
	}

	public static FileInfo GetFile(this DirectoryInfo dir, string filename)
	{
		return new FileInfo(Path.Combine(dir.FullName, filename));
	}

	public static DirectoryInfo GetSubdirectory(this DirectoryInfo dir, string name)
	{
		return new DirectoryInfo(Path.Combine(dir.FullName, name));
	}

	public static void EnsureExists(this DirectoryInfo dir)
	{
		dir.Refresh(); // экземпляр DirectoryInfo кэширует Exists
		if (!dir.Exists)
			dir.Create();
	}

	public static void ClearDirectory(this DirectoryInfo directory, bool deleteDirectory = false)
	{
		foreach (var file in directory.GetFiles())
			file.Delete();
		foreach (var subDirectory in directory.GetDirectories())
		{
			/* subDirectory.Delete(true) sometimes not works */
			subDirectory.ClearDirectory();
			subDirectory.Delete();
		}

		if (deleteDirectory)
			directory.Delete();
	}

	public static string GetRelativePath(this FileSystemInfo file, string folder)
	{
		var fileOrDirectoryPath = file.FullName;
		if (file is DirectoryInfo && !fileOrDirectoryPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
			fileOrDirectoryPath += Path.DirectorySeparatorChar;
		var pathUri = new Uri(fileOrDirectoryPath);
		if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
			folder += Path.DirectorySeparatorChar;
		var folderUri = new Uri(folder);
		if (folderUri.Equals(pathUri))
			return "";
		return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
	}

	public static string GetRelativePath(this FileSystemInfo fileOrDirectory, DirectoryInfo parentDirectory)
	{
		return fileOrDirectory.GetRelativePath(parentDirectory.FullName);
	}

	public static IEnumerable<string> GetRelativePathsOfFiles(this DirectoryInfo dir)
	{
		return Directory.EnumerateFiles(dir.FullName, "*", SearchOption.AllDirectories)
			.Select(path => new FileInfo(path))
			.Select(fi => fi.GetRelativePath(dir.FullName));
	}

	public static string[] GetFilenames(this DirectoryInfo di, string dirPath)
	{
		var dir = di.GetSubdirectory(dirPath);
		if (!dir.Exists)
			throw new Exception("No " + dirPath + " in " + di.Name);
		return dir.GetFiles().Select(f => Path.Combine(dirPath, f.Name)).OrderBy(f => f).ToArray();
	}
}