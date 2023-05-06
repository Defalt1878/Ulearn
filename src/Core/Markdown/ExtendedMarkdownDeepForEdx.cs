using System;
using System.IO;
using Ulearn.Core.Model.Edx.EdxComponents;

namespace Ulearn.Core.Markdown
{
	internal class ExtendedMarkdownDeepForEdx : ExtendedMarkdownDeep
	{
		private readonly DirectoryInfo courseDirectory;
		private readonly string edxBaseUrl;

		public ExtendedMarkdownDeepForEdx(MarkdownRenderContext context, DirectoryInfo courseDirectory, string edxBaseUrl)
			: base(context)
		{
			this.courseDirectory = courseDirectory;
			this.edxBaseUrl = edxBaseUrl;
		}

		public event Action<StaticFileForEdx> OnStaticFile;

		protected override string GetLinkToFile(string pathFromUnit)
		{
			var edxFileName = Path.Combine(context.UnitDirectoryRelativeToCourse, pathFromUnit).Replace("\\", "/").Replace("/", "_").Replace(" ", "_");
			var path = Path.Combine(courseDirectory.FullName, context.UnitDirectoryRelativeToCourse, pathFromUnit);
			var file = new FileInfo(path);
			OnStaticFile?.Invoke(new StaticFileForEdx(file, edxFileName));
			return Path.Combine(edxBaseUrl, edxFileName).Replace("\\", "/");
		}
	}
}