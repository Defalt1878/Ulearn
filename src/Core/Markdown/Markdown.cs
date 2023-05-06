using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using Ulearn.Core.Model.Edx.EdxComponents;

namespace Ulearn.Core.Markdown
{
	public record MarkdownRenderContext(string BaseUrlApi, string BaseUrlWeb, string CourseId, string UnitDirectoryRelativeToCourse);

	public static class Markdown
	{
		private static readonly Regex rxExtractLanguage = new("^({{(.+)}}[\r\n])", RegexOptions.Compiled);

		public static string RenderMarkdown(this string markdown, MarkdownRenderContext context)
		{
			var texReplacer = new TexReplacer(markdown);

			var markdownObject = new ExtendedMarkdownDeep(context)
			{
				NewWindowForExternalLinks = true,
				ExtraMode = true,
				SafeMode = false,
				MarkdownInHtml = false
			};

			markdownObject.FormatCodeBlock += FormatCodePrettyPrint;
			var html = markdownObject.Transform(texReplacer.ReplacedText);
			return texReplacer.PlaceTexInsertsBack(html);
		}

		public static (string Html, List<StaticFileForEdx> StaticFilesForEdx) RenderMarkdownForEdx(this string md, MarkdownRenderContext context,
			DirectoryInfo courseDirectory, string edxBaseUrl)
		{
			var texReplacer = new EdxTexReplacer(md);

			var markdownObject = new ExtendedMarkdownDeepForEdx(context, courseDirectory, edxBaseUrl)
			{
				NewWindowForExternalLinks = true,
				ExtraMode = true,
				SafeMode = false,
				MarkdownInHtml = false
			};

			var staticFiles = new List<StaticFileForEdx>();
			markdownObject.OnStaticFile += staticFiles.Add;

			markdownObject.FormatCodeBlock += FormatCodePrettyPrint;
			var html = markdownObject.Transform(texReplacer.ReplacedText);
			html = texReplacer.PlaceTexInsertsBack(html);
			return (html, staticFiles);
		}

		public static HtmlString RenderTex(this string textWithTex)
		{
			var texReplacer = new TexReplacer(textWithTex);
			var html = WebUtility.HtmlEncode(texReplacer.ReplacedText);
			return new HtmlString(texReplacer.PlaceTexInsertsBack(html));
		}

		private static string FormatCodePrettyPrint(MarkdownDeep.Markdown m, string code, string language)
		{
			code = code.TrimEnd();

			if (string.IsNullOrEmpty(language))
			{
				// Try to extract the language from the first line
				var match = rxExtractLanguage.Match(code);

				if (match.Success)
				{
					// Save the language
					var g = match.Groups[2];
					language = g.ToString();

					// Remove the first line
					code = code[match.Groups[1].Length..];
				}

				// If not specified, look for a link definition called "default_syntax" and
				// grab the language from its title
				if (language == null)
				{
					var d = m.GetLinkDefinition("default_syntax");
					if (d != null)
						language = d.title;
				}
			}

			// Common replacements
			if (language is "C#" or "cs")
				language = "csharp";
			if (language == "C++")
				language = "cpp";

			// Wrap code in pre/code tags and add PrettyPrint attributes if necessary
			return string.IsNullOrEmpty(language)
				? $"<pre><code>{code}</code></pre>\n"
				: $"<textarea class='code code-sample' data-lang='{language.ToLowerInvariant()}'>{code}</textarea>\n";
		}
	}
}