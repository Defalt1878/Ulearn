using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Vostok.Logging.Abstractions;

namespace AntiPlagiarism.Web.CodeAnalyzing
{
	public class TokensExtractor
	{
		// Pygmentize заменяет в токенах конец строки на \n. Заменим сами на \n, а потом восстановим в токенах
		private static readonly Regex lineEndingsRegex = new("\r\n|\n", RegexOptions.Compiled);
		private static ILog Log => LogProvider.Get().ForContext(typeof(TokensExtractor));

		private static IEnumerable<Token> FilterWhitespaceTokens(IEnumerable<Token> tokens)
		{
			return tokens.Where(t => !string.IsNullOrWhiteSpace(t.Value));
		}

		private static IEnumerable<Token> FilterCommentTokens(IEnumerable<Token> tokens)
		{
			return tokens.Where(t => !t.Type.StartsWith("Comment") || t.Type.StartsWith("Comment.Preproc"));
		}

		[NotNull]
		public static List<Token> GetFilteredTokensFromPygmentize(string code, Language language)
		{
			var tokens = GetAllTokensFromPygmentize(code, language).EmptyIfNull();
			return FilterCommentTokens(FilterWhitespaceTokens(tokens)).ToList();
		}

		[CanBeNull]
		public static List<Token> GetAllTokensFromPygmentize(string code, Language language)
		{
			var (codeWithNLineEndings, originalLineEndings) = PrepareLineEndingsForPygmentize(code);
			var pygmentizeResult = GetPygmentizeResult(codeWithNLineEndings, language);
			if (pygmentizeResult is null)
				return null;
			var tokensWithNLineEndings = ParseTokensFromPygmentize(pygmentizeResult).ToList();
			var tokens = ReturnOriginalLineEndings(tokensWithNLineEndings, originalLineEndings).ToList();
			SetPositions(tokens);
			return tokens;
		}

		private static IEnumerable<Token> ParseTokensFromPygmentize(string pygmentizeResult)
		{
			var lines = pygmentizeResult.TrimEnd().SplitToLines();
			foreach (var line in lines)
			{
				var parts = line.Split('\t', 2);
				var tokenType = parts[0];
				tokenType = tokenType["Token.".Length..];
				var tokenContentInQuotes = parts[1];
				var tokenContentEscaped = tokenContentInQuotes.Substring(1, parts[1].Length - 2);
				var tokenContent = Regex.Unescape(tokenContentEscaped);
				var token = new Token
				{
					Type = tokenType,
					Value = tokenContent
				};
				yield return token;
			}
		}

		private static (string code, List<string> originalLineEndings) PrepareLineEndingsForPygmentize(string code)
		{
			var originalLineEndings = lineEndingsRegex.Matches(code).Select(m => m.Value).ToList();
			var resultCode = code.Replace("\r\n", "\n");
			return (resultCode, originalLineEndings);
		}

		private static IEnumerable<Token> ReturnOriginalLineEndings(IReadOnlyList<Token> tokens, IReadOnlyList<string> originalLineEndings)
		{
			var lineNumber = 0;
			for (var i = 0; i < tokens.Count; i++)
			{
				var token = tokens[i];
				if (token.Value == "\n")
				{
					// pygmentize добавляет токен перевода строки в конце кода. Убираю его, если исходно не было
					if (originalLineEndings.Count == lineNumber && i == tokens.Count - 1)
						yield break;
					token.Value = originalLineEndings[lineNumber];
					lineNumber++;
					yield return token;
					continue;
				}

				var parts = token.Value.Split('\n'); // Если \n в конце, последняя часть будет пустой строкой
				var sb = new StringBuilder();
				foreach (var part in parts.SkipLast(1))
				{
					sb.Append(part);
					// pygmentize добавляет токен перевода строки в конце кода. Убираю его, если исходно не было
					if (originalLineEndings.Count == lineNumber && i == tokens.Count - 1)
						yield break;
					sb.Append(originalLineEndings[lineNumber]);
					lineNumber++;
				}

				sb.Append(parts.Last());
				token.Value = sb.ToString();
				yield return token;
			}
		}

		public static void ThrowExceptionIfTokensNotMatchOriginalCode(string code, List<Token> tokens)
		{
			if (tokens.Any(token => code.Substring(token.Position, token.Value.Length) != token.Value))
				throw new Exception();

			var allTokensContentLength = tokens.Sum(t => t.Value.Length);
			if (code.Length != allTokensContentLength)
				throw new Exception();
		}

		private static void SetPositions(IEnumerable<Token> tokens)
		{
			var pos = 0;
			foreach (var token in tokens)
			{
				token.Position = pos;
				pos += token.Value.Length;
			}
		}

		private static string GetPygmentizeResult(string code, Language language)
		{
			var lexer = language.GetAttribute<LexerAttribute>().Lexer;
			var arguments = lexer is null ? "-g" : $"-l {lexer}";
			arguments += " -f tokens -O encoding=utf-8";

			var sw = Stopwatch.StartNew();
			using var process = BuildProcess(arguments);
			process.Start();
			
			const int limit = 10 * 1024 * 1024;
			var utf8StandardErrorReader = new StreamReader(process.StandardError.BaseStream, Encoding.UTF8);
			var utf8StandardOutputReader = new StreamReader(process.StandardOutput.BaseStream, Encoding.UTF8);
			var readErrTask = new AsyncReader(utf8StandardErrorReader, limit).GetDataAsync();
			var readOutTask = new AsyncReader(utf8StandardOutputReader, limit).GetDataAsync();
			process.StandardInput.BaseStream.Write(Encoding.UTF8.GetBytes(code));
			process.StandardInput.BaseStream.Close();
			var isFinished = Task.WaitAll(new Task[] { readErrTask, readOutTask }, 1000);
			var ms = sw.ElapsedMilliseconds;

			if (!process.HasExited)
				Shutdown(process);

			if (readErrTask.Result.Length > 0)
				Log.Warn($"pygmentize написал на stderr: {readErrTask.Result}");

			if (!isFinished)
				Log.Warn($"Не хватило времени ({ms} ms) на работу pygmentize");
			else
				Log.Info($"pygmentize закончил работу за {ms} ms");

			if (process.ExitCode != 0)
				Log.Info($"pygmentize завершился с кодом {process.ExitCode}");

			return isFinished && readErrTask.Result.Length == 0 && readOutTask.Result.Length > 0
				? readOutTask.Result
				: null;
		}

		private static void Shutdown(Process process)
		{
			try
			{
				process.Kill();
			}
			catch (Win32Exception)
			{
				/* Sometimes we can catch Access Denied error because the process is already terminating. It's ok, we don't need to rethrow exception */
			}
			catch (InvalidOperationException)
			{
				/* If process has already terminated */
			}

			var remainingTimeoutMs = 3000;
			while (!process.HasExited)
			{
				const int time = 10;
				Thread.Sleep(time);
				remainingTimeoutMs -= time;
				if (remainingTimeoutMs <= 0)
					throw new Exception($"process {process.Id} is not completed after kill");
			}
		}

		private static Process BuildProcess(string arguments)
		{
			return new Process
			{
				StartInfo =
				{
					Arguments = arguments,
					FileName = "pygmentize",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					CreateNoWindow = true,
					UseShellExecute = false
				}
			};
		}
	}
}