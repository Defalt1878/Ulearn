using System;
using System.IO;
using Ulearn.Common;

namespace Ulearn.Core.Helpers
{
	public class TempFile : IDisposable
	{
		public readonly FileInfo FileInfo;

		public TempFile(string fileName, Stream stream)
		{
			var path = Path.Combine(TempDirectory.TempDirectoryPath, fileName);
			using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
			{
				stream.CopyTo(file);
			}

			FileInfo = new FileInfo(path);
		}

		public void Dispose()
		{
			try
			{
				FuncUtils.TrySeveralTimes(() => FileInfo.Delete(), 3);
				GC.SuppressFinalize(this);
			}
			catch (Exception ex)
			{
				// ignore
			}
		}
	}
}