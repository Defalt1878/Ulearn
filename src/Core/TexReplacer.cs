using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace Ulearn.Core
{
	public class TexReplacer
	{
		public enum InsertionType
		{
			Span,
			Div
		}

		private static readonly Regex texDivRegex = new(@"(^|\W)\$\$([^\$]+?)\$\$($|\W)", RegexOptions.Multiline);
		private static readonly Regex texSpanRegex = new(@"(^|\W)\$([^\$]+?)\$($|\W)", RegexOptions.Multiline);
		private static readonly Regex emptyParaRegex = new(@"<p>\s*</p>");
		private readonly Regex backRegex;
		private readonly string id;
		private readonly Dictionary<string, Tuple<string, InsertionType>> texInserts = new();
		private int counter;

		public TexReplacer(string text)
		{
			id = Guid.NewGuid().ToString("N");
			backRegex = new Regex(id + @"\[\d+\]");
			ReplacedText = ReplaceTexInserts(text);
		}

		public string ReplacedText { get; }

		private string ReplaceTexInserts(string md)
		{
			var result = texDivRegex.Replace(md ?? "", m => MakeInsertId(m, InsertionType.Div), int.MaxValue);
			result = texSpanRegex.Replace(result, m => MakeInsertId(m, InsertionType.Span));
			result = result.Replace(" -- ", " — ");
			result = result.Replace("&#36;", "$");
			result = result.Replace("&amp;#36;", "&#36;");
			return result;
		}

		public string PlaceTexInsertsBack(string textWithIds)
		{
			var html = backRegex.Replace(textWithIds, PrepareTexInsert);
			return emptyParaRegex.Replace(html, "");
		}

		private string PrepareTexInsert(Match match)
		{
			var insertId = match.Value;
			if (!texInserts.TryGetValue(insertId, out var tex))
				return insertId;
			if (tex.Item2 == InsertionType.Div)
				return FormatTexDiv(tex.Item1);
			if (tex.Item2 == InsertionType.Span)
				return FormatTexSpan(tex.Item1);
			throw new Exception(tex.Item2.ToString());
		}

		protected virtual string FormatTexSpan(string tex)
		{
			return "<span class='tex'>" + WebUtility.HtmlEncode(tex) + "</span>";
		}

		protected virtual string FormatTexDiv(string tex)
		{
			return "</p><div class='tex'>\\displaystyle " + WebUtility.HtmlEncode(tex) + "</div><p>";
		}

		private string MakeInsertId(Match match, InsertionType insertionType)
		{
			var insertId = id + "[" + counter++ + "]";
			texInserts.Add(insertId, Tuple.Create(match.Groups[2].Value, insertionType));
			return match.Groups[1].Value + "" + insertId + match.Groups[3].Value;
		}
	}

	public class EdxTexReplacer : TexReplacer
	{
		public EdxTexReplacer(string text)
			: base(text)
		{
		}

		public static string ReplaceTex(string text)
		{
			text = text.Replace("`", "");
			var replacer = new EdxTexReplacer(text);
			return replacer.PlaceTexInsertsBack(replacer.ReplacedText);
		}

		protected override string FormatTexSpan(string tex)
		{
			return "`" + WebUtility.HtmlEncode(tex) + "`";
		}

		protected override string FormatTexDiv(string tex)
		{
			return "[mathjax]" + WebUtility.HtmlEncode(tex) + "[/mathjax]";
		}
	}
}