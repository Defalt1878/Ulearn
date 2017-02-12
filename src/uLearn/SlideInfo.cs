﻿using System;
using System.IO;

namespace uLearn
{
	public class SlideInfo
	{
		public int Index { get; set; }
		public string UnitName { get; private set; }
		public FileInfo SlideFile { get; set; }
		public DirectoryInfo Directory => SlideFile.Directory;

		public SlideInfo(string unitName, FileInfo slideFile, int index)
		{
			Index = index;
			UnitName = unitName;
			SlideFile = slideFile;
		}

		public string DirectoryRelativePath => CourseUnitUtils.GetDirectoryRelativeWebPath(SlideFile);
	}

	public class CourseUnitUtils
	{
		public static string GetDirectoryRelativeWebPath(FileInfo file)
		{
			var path = "/";
			var d = file.Directory;
			while (d != null)
			{
				path = $"/{d.Name}{path}";
				if (d.Name.Equals("Courses", StringComparison.OrdinalIgnoreCase))
					return path;
				d = d.Parent;
			}
			throw new FileNotFoundException("Courses folder not found", file.FullName);
		}
	}
}
