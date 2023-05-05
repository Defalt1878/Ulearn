using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnidecodeSharpCore;

namespace Ulearn.Common.Extensions;

public static class StringExtensions
{
	public static string[] GetFiles(this string directoryPath)
	{
		return Directory.Exists(directoryPath) ? Directory.GetFiles(directoryPath) : Array.Empty<string>();
	}

	public static string PathCombine(this string directoryPath, string subdirectoryPath)
	{
		return Path.Combine(directoryPath, subdirectoryPath);
	}

	public static string[] GetFiles(this string directoryPath, string pattern)
	{
		return Directory.Exists(directoryPath) ? Directory.GetFiles(directoryPath, pattern) : Array.Empty<string>();
	}

	public static string GetSingleFile(this string directoryPath, string pattern)
	{
		var files = Directory.GetFiles(directoryPath, pattern);
		if (files.Length == 0)
			throw new Exception($"No such file {directoryPath}/{pattern}");
		if (files.Length > 1)
			throw new Exception($"More than one file {directoryPath}/{pattern}: {string.Join(", ", files)}");
		return files[0];
	}

	public static string ExcludeLinesWith(this string s, params string[] args)
	{
		return string.Join(
			Environment.NewLine,
			s.SplitToLines().Where(line => !args.Any(line.Contains)));
	}

	public static string WithArgs(this string s, params object[] args)
	{
		return string.Format(s, args);
	}

	public static string RemoveBom(this string text)
	{
		return text.TrimStart('\uFEFF', '\u200B');
	}

	public static string AsUtf8(this byte[] bytes)
	{
		return Encoding.UTF8.GetString(bytes).RemoveBom();
	}

	public static string[] SplitToLines(this string text)
	{
		return Regex.Split(text, @"\r?\n");
	}

	public static string[] SplitToLinesWithEoln(this string text)
	{
		return Regex.Split(text, @"(?<=\r?\n)").Where(s => !string.IsNullOrEmpty(s)).ToArray();
	}

	public static IEnumerable<string> SplitByCamelCase(this string text)
	{
		var sb = new StringBuilder();
		foreach (var letter in text)
		{
			if ((char.IsUpper(letter) || !char.IsLetter(letter)) && sb.Length != 0)
			{
				yield return sb.ToString();
				sb.Clear();
			}

			if (char.IsLetter(letter))
				sb.Append(letter);
		}

		if (sb.Length != 0)
			yield return sb.ToString();
	}

	public static string RemoveEmptyLinesFromStart(this string text)
	{
		var lines = text
			.SplitToLines()
			.SkipWhile(string.IsNullOrWhiteSpace);
		return string.Join("\r\n", lines);
	}

	public static string RemoveCommonNesting(this string text)
	{
		var lines = text.SplitToLines();
		var newLines = lines.RemoveCommonNesting();
		return string.Join("\r\n", newLines);
	}

	public static string LineEndingsToUnixStyle(this string text)
	{
		return string.Join("\n", text.SplitToLines());
	}

	public static string NormalizeEoln(this string str)
	{
		return str.LineEndingsToUnixStyle().Trim();
	}

	public static string EnsureEnoughLines(this string text, int minLinesCount)
	{
		var lines = text.SplitToLines();
		return string.Join("\n", lines.Concat(Enumerable.Repeat("", Math.Max(0, minLinesCount - lines.Length))));
	}

	public static IEnumerable<string> RemoveCommonNesting(this IList<string> lines)
	{
		var nonEmptyLines = lines.Where(line => line.Trim().Length > 0).ToList();
		if (nonEmptyLines.Any())
		{
			var nesting = nonEmptyLines.Min(line => line.TakeWhile(char.IsWhiteSpace).Count());
			var newLines = lines.Select(line => line.Length > nesting ? line[nesting..] : line);
			return newLines;
		}

		return Enumerable.Repeat("", lines.Count);
	}

	public static string FixExtraEolns(this string arg)
	{
		return Regex.Replace(arg.Trim(), "(\t*\r?\n){3,}", "\r\n\r\n");
	}

	public static string ToLatin(this string arg)
	{
		const char nonLatinCharsReplacement = '_';

		arg = arg.Unidecode();
		var result = "";
		foreach (var c in arg)
			if (char.IsLetterOrDigit(c))
				result += c;
			else if (result.Length == 0 || result[^1] != nonLatinCharsReplacement)
				result += nonLatinCharsReplacement;

		return result;
	}

	public static Guid ToDeterministicGuid(this string arg, Encoding encoding = null)
	{
		encoding = encoding ?? Encoding.UTF8;
		using var md5 = MD5.Create();
		var hash = md5.ComputeHash(encoding.GetBytes(arg));
		return new Guid(hash);
	}

	public static string CalculateMd5(this string arg)
	{
		byte[] hash;
		using (var md5 = MD5.Create())
		{
			hash = md5.ComputeHash(Encoding.UTF8.GetBytes(arg));
		}

		var sb = new StringBuilder();
		foreach (var b in hash)
			sb.Append(b.ToString("X2"));
		return sb.ToString();
	}

	public static string MaskAsSecret(this string str)
	{
		if (str.Length < 5)
			return string.Join("", Enumerable.Repeat("*", str.Length));

		return str[..2] + string.Join("", Enumerable.Repeat("*", str.Length - 4)) + str[^2..];
	}

	public static string EscapeMarkdown(this string text)
	{
		return Regex.Replace(text, @"([\[\]|\*_`])", @"\$1");
	}

	public static string EscapeHtml(this string text)
	{
		return SecurityElement.Escape(text);
	}

	public static string MakeNestedQuotes(this string text)
	{
		text = Regex.Replace(text, "(\\s|^)[\"«]", @"$1„");
		return Regex.Replace(text, "[\"»]", @"“");
	}

	public static string RenderSimpleMarkdown(this string text, bool isHtml = true, bool telegramMode = false)
	{
		text = Regex.Replace(text, @"\*\*(.+?)\*\*", @"<b>$1</b>", RegexOptions.Multiline);
		text = Regex.Replace(text, @"__(.+?)__", @"<i>$1</i>", RegexOptions.Multiline);

		var newLineRegExp = isHtml ? @"\<br/?\>" : @"\r?\n";
		/* ```Multiline code``` */
		text = Regex.Replace(text, @"(?:" + newLineRegExp + @")*```(?:" + newLineRegExp + @")*(.+?)```(?:" + newLineRegExp + @")*", @"<pre>$1</pre>", RegexOptions.Multiline | RegexOptions.Singleline);
		/* `Inline code` */
		text = Regex.Replace(text, @"`([^`\n\r]+)`", telegramMode ? @"<code>$1</code>" : @"<span class='inline-pre'>$1</span>", RegexOptions.Multiline);

		return text;
	}

	public static string LineEndingsToBrTags(this string text)
	{
		return Regex.Replace(text, @"\r?\n", "<br/>");
	}

	public static string ToLowerFirstLetter(this string text)
	{
		if (string.IsNullOrEmpty(text))
			return "";
		return char.ToLowerInvariant(text[0]) + text[1..];
	}

	public static bool EqualsIgnoreCase(this string first, string second)
	{
		return string.Equals(first, second, StringComparison.InvariantCultureIgnoreCase);
	}

	public static string EncodeQuotes(this string text)
	{
		return text.Replace(@"\", @"\\").Replace(@"""", @"\""").Replace("'", @"\'");
	}

	public static string NullIfEmptyOrWhitespace(this string str)
	{
		return string.IsNullOrWhiteSpace(str) ? null : str;
	}
}