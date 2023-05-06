using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NHunspell;
using Ulearn.Common.Extensions;
using Ulearn.Core.Properties;

namespace Ulearn.Core.CSharp.Validators.SpellingValidator
{
	public class CodeElementSpellChecker
	{
		private static readonly Hunspell hunspellEnUs = new(Resources.en_US_aff, Resources.en_US_dic);
		private static readonly Hunspell hunspellEnGb = new(Resources.en_GB_aff, Resources.en_GB_dic);
		private static readonly Hunspell hunspellLa = new(Resources.la_aff, Resources.la_dic);
		private static readonly HashSet<string> wordsToExcept = new(Resources.spelling_exceptions.SplitToLines());

		public bool CheckIdentifierNameForSpellingErrors(string identifierName, string typeAsString = null)
		{
			var wordsInIdentifier = identifierName.SplitByCamelCase();
			foreach (var word in wordsInIdentifier)
			{
				var wordForCheck = RemoveIfySuffix(word);
				var wordInDifferentNumbers = GetWordInDifferentNumbers(wordForCheck);
				var doesWordContainError = true;
				foreach (var wordInDifferentNumber in wordInDifferentNumbers)
				{
					if (CheckForSpecialCases(wordInDifferentNumber, typeAsString)
						|| IsWordContainedInDictionaries(wordInDifferentNumber))
					{
						doesWordContainError = false;
						break;
					}

					var wordContainsError = CheckConcatenatedWordsInLowerCaseForError(wordInDifferentNumber);
					if (wordContainsError)
						continue;
					doesWordContainError = false;
					break;
				}

				if (doesWordContainError)
					return true;
			}

			return false;
		}

		private static bool CheckForSpecialCases(string word, string typeAsString = null)
		{
			return word.Length <= 2
					|| Regex.IsMatch(word, @"\p{IsCyrillic}")
					|| (typeAsString != null && typeAsString.StartsWith(word, StringComparison.InvariantCultureIgnoreCase))
					|| word.Equals(typeAsString?.MakeTypeNameAbbreviation(), StringComparison.InvariantCultureIgnoreCase);
		}

		private static string RemoveIfySuffix(string word)
		{
			return word.LastIndexOf("ify", StringComparison.InvariantCultureIgnoreCase) > 0
				? word[..^3]
				: word;
		}

		private static IEnumerable<string> GetWordInDifferentNumbers(string word)
		{
			yield return word;
			if (word.Length > 1 && word.EndsWith("s", StringComparison.InvariantCultureIgnoreCase))
				yield return word[..^1];
			if (word.Length > 2 && word.EndsWith("es", StringComparison.InvariantCultureIgnoreCase))
				yield return word[..^2];
		}

		private bool CheckConcatenatedWordsInLowerCaseForError(string concatenatedWords)
		{
			var currentCheckingWord = "";
			var foundWords = new HashSet<string>();
			while (currentCheckingWord.Length != concatenatedWords.Length)
			{
				currentCheckingWord = "";
				var isFirstWord = true;
				foreach (var symbol in concatenatedWords)
				{
					currentCheckingWord += symbol;
					if (!foundWords.Contains(currentCheckingWord) &&
						((currentCheckingWord.Length == 1 && isFirstWord) ||
						(currentCheckingWord.Length != 1 && IsWordContainedInDictionaries(currentCheckingWord))))
					{
						foundWords.Add(currentCheckingWord);
						if (isFirstWord)
							if (IsWordContainedInDictionaries(
									concatenatedWords[currentCheckingWord.Length..])) // это нужно для переменных типа dfunction b substring
								return false;

						currentCheckingWord = "";
						isFirstWord = false;
					}
				}

				if (currentCheckingWord == "")
					return false;
			}

			return true;
		}

		private static bool IsWordContainedInDictionaries(string word) // есть проблема, при которой разные части одного слова проверяются в разных словарях (одна часть - в английском, вторая - в латинском)?
		{
			return wordsToExcept.Contains(word.ToLowerInvariant())
					|| hunspellEnUs.Spell(word)
					|| hunspellEnGb.Spell(word)
					|| hunspellLa.Spell(word);
		}
	}
}