using System.IO;
using NUnit.Framework;

namespace Ulearn.Common.Extensions
{
	[TestFixture]
	public class FileSystemExtensions_should
	{
		private const string path = @"C:\git\Ulearn\src\Courses\Courses\ana";

		[Test]
		public void GetRelativePathToTheSameDirectory()
		{
			var unitDirectory = new DirectoryInfo(path);
			var courseDirectory = new DirectoryInfo(path);
			var relativePath = unitDirectory.GetRelativePath(courseDirectory);
			Assert.AreEqual("", relativePath);
		}

		[Test]
		public void GetRelativePathToFile()
		{
			var file = new FileInfo(Path.Combine(path, "1.txt"));
			var courseDirectory = new DirectoryInfo(path);
			var relativePath = file.GetRelativePath(courseDirectory);
			Assert.AreEqual("1.txt", relativePath);
		}

		[Test]
		public void GetRelativePathToDirectory()
		{
			var directory = new DirectoryInfo(Path.Combine(path, "dir"));
			var courseDirectory = new DirectoryInfo(path);
			var relativePath = directory.GetRelativePath(courseDirectory);
			Assert.AreEqual(@"dir\", relativePath);
		}
	}
}