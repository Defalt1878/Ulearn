using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace Ulearn.Common;

public static class EnvironmentVariablesUtils
{
	public static bool ExistsOnPath(string fileName)
	{
		return GetFullPath(fileName) is not null;
	}

	[CanBeNull]
	private static string GetFullPath(string fileName)
	{
		if (File.Exists(fileName))
			return Path.GetFullPath(fileName);

		var values = Environment.GetEnvironmentVariable("PATH");
		return values?.Split(Path.PathSeparator)
			.Select(path => Path.Combine(path, fileName!))
			.FirstOrDefault(File.Exists);
	}
}