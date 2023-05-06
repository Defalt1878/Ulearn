using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NHunspell;
using Ulearn.Common.Extensions;
using Ulearn.Core.Properties;

namespace Ulearn.Core.SpellChecking
{
	public class SpellingError
	{
		public string Misspelling { get; set; }

		public string Context { get; set; }

		public string[] Suggestions { get; set; }

		private bool Equals(SpellingError other)
		{
			return string.Equals(Misspelling, other.Misspelling);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != GetType())
				return false;
			return Equals((SpellingError)obj);
		}

		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			return Misspelling?.GetHashCode() ?? 0;
		}

		public override string ToString()
		{
			return $"{nameof(Misspelling)}: {Misspelling}, {nameof(Context)}: {Context}, {nameof(Suggestions)}: {string.Join(", ", Suggestions)}";
		}
	}

	public class SpellChecker : IDisposable
	{
		private readonly Regex camelCaseRegEx = new(@"[А-Яа-я][a-я]*(([А-Я][а-я]*)+)", RegexOptions.Compiled);

		private readonly Hunspell hunspell;

		private readonly string[] prefixes;

		private readonly Regex wordRegEx = new(@"\b(?<word>\w+)(?<dotEnding>\.\s*)?", RegexOptions.Compiled);

		public SpellChecker(string customDictionaryPath)
		{
			prefixes = Resources.customPrefixes.SplitToLines();
			hunspell = new Hunspell(Encoding.UTF8.GetBytes(Resources.ru_RU_aff), Encoding.UTF8.GetBytes(Resources.ru_RU_dic));

			InitializeInternalCustomDictionary(hunspell);

			if (!string.IsNullOrEmpty(customDictionaryPath))
				InitializeCustomDictionary(hunspell, customDictionaryPath);
		}

		public SpellChecker()
			: this("")
		{
		}

		public void Dispose()
		{
			hunspell.Dispose();
			GC.SuppressFinalize(this);
		}

		private static void InitializeCustomDictionary(Hunspell hunspell, string[] dictionaryLines)
		{
			foreach (var dictionaryLine in dictionaryLines)
			{
				if (string.IsNullOrEmpty(dictionaryLine) || dictionaryLine.StartsWith("#"))
					continue;

				var tokens = dictionaryLine.Split(':');
				if (tokens.Length == 1)
					hunspell.Add(tokens[0].Trim());
				else
					hunspell.AddWithAffix(tokens[0].Trim(), tokens[1].Trim());
			}
		}

		private static void InitializeCustomDictionary(Hunspell hunspell, string customDictionaryPath)
		{
			var dictionaryLines = File.ReadAllLines(customDictionaryPath);
			InitializeCustomDictionary(hunspell, dictionaryLines);
		}

		private static void InitializeInternalCustomDictionary(Hunspell hunspell)
		{
			var dictionaryLines = Resources.customDictionary.SplitToLines();
			InitializeCustomDictionary(hunspell, dictionaryLines);
		}

		public void AddWord(string customWord)
		{
			hunspell.Add(customWord);
		}

		public void AddWord(string customWord, string similarWord)
		{
			hunspell.AddWithAffix(customWord, similarWord);
		}

		public SpellingError[] SpellCheckString(string str)
		{
			return SpellCheckString(str, "");
		}

		private SpellingError[] SpellCheckString(string str, string context)
		{
			var wordCandidates = wordRegEx.Matches(str).Cast<Match>();
			var wordList = new HashSet<string>();

			foreach (var wordCandidate in wordCandidates)
			{
				var word = wordCandidate.Groups["word"].Value.Replace('Ё', 'Е').Replace('ё', 'е');
				var dotEnding = wordCandidate.Groups["dotEnding"];

				//we will check the word only if it is not shortening
				if (!dotEnding.Success)
				{
					wordList.Add(word);
				}
				else
				{
					//dot was sentence sign
					var nextCharIndex = dotEnding.Index + dotEnding.Length;
					if (nextCharIndex >= str.Length || char.IsUpper(str[nextCharIndex]))
						wordList.Add(word);
				}
			}

			var spellingErrors = wordList.Select(x => SpellWord(x, context))
				.Where(spellingError => spellingError != null)
				.ToArray();
			return spellingErrors;
		}


		private SpellingError SpellWord(string word, string context)
		{
			//no any russian letter - ignore it
			if (!Regex.IsMatch(word, @"\p{IsCyrillic}"))
				return null;

			//digits in word - it is not word
			if (word.Any(char.IsDigit))
				return null;

			//WORD_WITH_UNDERSCORE - probably xml tag
			if (word.Contains('_'))
				return null;

			var normalizedWord = new string(word.Where(IsRussianLetter).ToArray());

			//two-letters word - ignore it, who knows what they mean
			if (normalizedWord.Length <= 2)
				return null;

			//it's not word
			if (normalizedWord.Length > 40)
				return null;

			// SomeWordInCamelCase - ignore it
			if (camelCaseRegEx.IsMatch(normalizedWord) && normalizedWord.Any(char.IsLower))
				return null;

			var abbreviationPartLength = normalizedWord.TakeWhile(char.IsUpper).Count();
			// it is abbreviation or its wordform - ignore it
			if (abbreviationPartLength >= 3)
			{
				var suffixSize = normalizedWord.Length - abbreviationPartLength;
				var wordFormPartExist = suffixSize is >= 1 and <= 2;
				var appropriateSize = suffixSize == 0 && abbreviationPartLength <= 5;
				if (wordFormPartExist || appropriateSize)
					return null;
			}

			var lowercasedNormalizedWord = normalizedWord.ToLower();
			if (hunspell.Spell(normalizedWord) || hunspell.Spell(lowercasedNormalizedWord))
				return null;

			var isPrefixedWord = prefixes.Any(
				prefix => lowercasedNormalizedWord.StartsWith(prefix)
						&& hunspell.Spell(lowercasedNormalizedWord[prefix.Length..]));
			if (isPrefixedWord)
				return null;

			var suggests = hunspell.Suggest(normalizedWord);

			//One does not simply make error without suggestions. It's not word at all
			if (suggests.Count == 0)
				return null;

			return new SpellingError
			{
				Misspelling = word,
				Suggestions = suggests.ToArray(),
				Context = context
			};
		}

		private static bool IsRussianLetter(char c)
		{
			return BelongsToRange(c, 'а', 'я') || BelongsToRange(c, 'А', 'Я') || c == 'ё' || c == 'Ё';
		}

		private static bool BelongsToRange(char c, char left, char right)
		{
			return (uint)(c - left) <= right - left;
		}
	}
}