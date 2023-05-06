using System;
using System.Text.RegularExpressions;
using MarkdownDeep;
using Ulearn.Core.Courses;

namespace Ulearn.Core.Markdown
{
	internal class ExtendedMarkdownDeep : MarkdownDeep.Markdown
	{
		private readonly Regex fileLinkRegex = new(@".*\.\w{1,5}(?:$|#|\?|&)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private readonly Regex fileToDownloadLinkRegex = new(@".*\.(zip|odp|pptx|docx|xlsx|pdf|mmap|xmind)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		protected readonly MarkdownRenderContext context;

		public ExtendedMarkdownDeep(MarkdownRenderContext context)
		{
			this.context = context;
		}

		public override void OnPrepareLink(HtmlTag tag)
		{
			base.OnPrepareLink(tag);
			var href = tag.Attributes["href"];
			if (href.StartsWith("./"))
				href = href[2..];

			var isFileToDownload = fileToDownloadLinkRegex.IsMatch(href);
			if (isFileToDownload)
				tag.Attributes["download"] = "";

			if (!IsAbsoluteUrl(href))
			{
				if (!href.StartsWith("/") && IsFile(href))
					tag.Attributes["href"] = GetLinkToFile(href);
				else
					tag.Attributes["href"] = href;
			}
			else
			{
				if (IsUlearnUrl(href))
					tag.Attributes.Remove("target");
			}
		}

		public override void OnPrepareImage(HtmlTag tag, bool titledImage)
		{
			base.OnPrepareImage(tag, titledImage);
			tag.Attributes["class"] = "slide-image";

			var src = tag.Attributes["src"];
			if (!IsAbsoluteUrl(src))
				tag.Attributes["src"] = GetLinkToFile(src);
		}

		protected virtual string GetLinkToFile(string pathFromUnit)
		{
			return CourseUrlHelper.GetAbsoluteUrlToFile(context.BaseUrlApi, context.CourseId, context.UnitDirectoryRelativeToCourse, pathFromUnit);
		}

		private static bool IsAbsoluteUrl(string url)
		{
			return Uri.TryCreate(url, UriKind.Absolute, out _);
		}

		private bool IsUlearnUrl(string url)
		{
			return url.Contains(context.BaseUrlApi, StringComparison.OrdinalIgnoreCase) ||
					url.Contains(context.BaseUrlWeb, StringComparison.OrdinalIgnoreCase);
		}

		private bool IsFile(string url)
		{
			return fileLinkRegex.IsMatch(url);
		}
	}
}