using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ulearn.Common.Extensions;

namespace Ulearn.Common
{
	public class LanguageLaunchInfo
	{
		public string Compiler { get; set; }
		public string CompileCommand { get; set; }
		public string RunCommand { get; set; }
	}

	[JsonConverter(typeof(StringEnumConverter), true)]
	public enum Language : short
	{
		[XmlEnum("csharp")]
		[Lexer("csharp")]
		[CommentSymbols("//")]
		CSharp = 1,

		[XmlEnum("python2")]
		[Lexer("py2")]
		[CommentSymbols("#")]
		Python2 = 2,

		[XmlEnum("python3")]
		[Lexer("py3")]
		[CommentSymbols("#")]
		Python3 = 3,

		[XmlEnum("java")]
		[Lexer("java")]
		[CommentSymbols("//")]
		Java = 4,

		[XmlEnum("javascript")]
		[Lexer("js")]
		[CommentSymbols("//")]
		JavaScript = 5,

		[XmlEnum("html")]
		[Lexer("html")]
		Html = 6,

		[XmlEnum("typescript")]
		[Lexer("ts")]
		[CommentSymbols("//")]
		TypeScript = 7,

		[XmlEnum("css")]
		[Lexer("css")]
		Css = 8,

		[XmlEnum("haskell")]
		[Lexer("haskell")]
		[CommentSymbols("--")]
		Haskell = 9,

		[XmlEnum("cpp")]
		[Lexer("cpp")]
		[CommentSymbols("//")]
		Cpp = 10,

		[XmlEnum("c")]
		[Lexer("c")]
		[CommentSymbols("//")]
		C = 11,

		[XmlEnum("pgsql")]
		[Lexer("postgresql")]
		[CommentSymbols("--")]
		PgSql = 12,

		[XmlEnum("mikrokosmos")]
		[Lexer("py3")]
		[CommentSymbols("#")]
		Mikrokosmos = 13, // https://mroman42.github.io/mikrokosmos/userguide.html

		[XmlEnum("hdl")]
		[Lexer("text")]
		[CommentSymbols("//")]
		Hdl = 14, // https://www.nand2tetris.org/project01 https://drive.google.com/file/d/1dPj4XNby9iuAs-47U9k3xtYy9hJ-ET0T/view

		[XmlEnum("text")]
		[Lexer("text")]
		[CommentSymbols("//")]
		Text = 100
	}

	public class LexerAttribute : Attribute
	{
		public readonly string Lexer;

		public LexerAttribute(string lexer)
		{
			Lexer = lexer;
		}
	}

	public class CommentSymbolsAttribute : Attribute
	{
		public readonly string CommentSymbols;

		public CommentSymbolsAttribute(string commentSymbols)
		{
			CommentSymbols = commentSymbols;
		}
	}

	public static class LanguageHelpers
	{
		private static readonly Dictionary<string, Language> extensions = new()
		{
			{ ".cs", Language.CSharp },
			{ ".py", Language.Python3 },
			{ ".py2", Language.Python2 },
			{ ".py3", Language.Python3 },
			{ ".html", Language.Html },
			{ ".css", Language.Css },
			{ ".js", Language.JavaScript },
			{ ".ts", Language.TypeScript },
			{ ".java", Language.Java },
			{ ".hs", Language.Haskell },
			{ ".c", Language.C },
			{ ".cpp", Language.Cpp },
			{ ".sql", Language.PgSql },
			{ ".mkr", Language.Mikrokosmos },
			{ ".hdl", Language.Hdl },
			{ ".txt", Language.Text }
		};

		public static Language GuessByExtension(string extension)
		{
			if (extensions.TryGetValue(extension, out var language))
				return language;

			throw new ArgumentException(
				$"Can't guess programming language by file extension. Unknown file extension: {extension}\n" +
				$"Known extensions are {string.Join(", ", extensions.Keys)}."
			);
		}

		public static Language GuessByExtension(FileInfo file)
		{
			return GuessByExtension(file.Extension);
		}

		public static Language ParseFromXml(string language)
		{
			var serializer = new XmlSerializer(typeof(Language));

			/* XmlSerializer expects XML so wrap what you got in xml tags */
			var xmlBytes = Encoding.ASCII.GetBytes($"<Language>{language.EscapeHtml()}</Language>");
			using var ms = new MemoryStream(xmlBytes);
			return (Language)serializer.Deserialize(ms)!;
		}

		public static Language ParseByName(string language)
		{
			return ParseFromXml(language);
		}

		public static bool TryParseByName(string language, out Language value)
		{
			try
			{
				value = ParseByName(language);
				return true;
			}
			catch (Exception)
			{
				value = Language.Text;
				return false;
			}
		}
	}

	public static class LanguageExtensions
	{
		public static string GetName(this Language language)
		{
			return language.GetXmlEnumName();
		}

		public static string GetName(this Language? language)
		{
			return language.HasValue ? language.Value.GetName() : "";
		}
	}
}