using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.SpellChecking;

namespace Ulearn.Core.Extensions
{
	public static class SpellCheckExtensions
	{
		public static string[] SpellCheck(this Course course, string courseDirectory)
		{
			var dictionaryPath = course.TryGetDictionaryPath(courseDirectory);
			if (string.IsNullOrEmpty(dictionaryPath))
				return Array.Empty<string>();

			using var spellchecker = new SpellChecker(dictionaryPath);
			return spellchecker.SpellCheckCourse(course);
		}

		public static string[] SpellCheckCourse(this SpellChecker spellchecker, Course course)
		{
			var titleErrors = spellchecker.SpellCheckString(course.Title).Select(e => e.ToPrettyString()).ToList();
			var titleError = ToPrettyMessage("Заголовок курса:", titleErrors);

			var unitsTitlesErrors = course.GetUnitsNotSafe()
				.Select(u => u.Title)
				.SelectMany(spellchecker.SpellCheckString)
				.Select(e => e.ToPrettyString())
				.ToList();
			var unitsTitlesError = ToPrettyMessage("Заголовки модулей:", unitsTitlesErrors);

			var slidesErrors = course.GetSlidesNotSafe().Select(spellchecker.SpellCheckSlide).Where(s => !string.IsNullOrWhiteSpace(s));

			var res = new List<string> { titleError, unitsTitlesError };
			res.AddRange(slidesErrors);
			return res.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
		}

		public static string SpellCheckSlide(this SpellChecker spellchecker, Slide slide)
		{
			var prefix = $"{slide.Title} ({slide.NormalizedGuid}):";
			var titleErrors = spellchecker.SpellCheckString(slide.Title);
			var blocksErrors = slide.Blocks.Select(b => b.TryGetText()).Where(s => !string.IsNullOrWhiteSpace(s)).SelectMany(spellchecker.SpellCheckString);
			var errorsList = titleErrors.Concat(blocksErrors).Select(e => e.ToPrettyString()).ToList();
			return ToPrettyMessage(prefix, errorsList);
		}

		public static string ToPrettyString(this SpellingError error)
		{
			return $"Найдено '{error.Misspelling}', возможные варианты: [{string.Join(", ", error.Suggestions)}]";
		}

		private static string ToPrettyMessage(string title, IList<string> errors)
		{
			if (!errors.Any())
				return null;
			var errorsString = string.Join("\n", errors.Select(s => '\t' + s));
			return title + '\n' + errorsString;
		}

		private static string TryGetDictionaryPath(this Course course, string courseDirectory)
		{
			if (course.Settings.DictionaryFile == null)
				return null;

			var file = Path.Combine(courseDirectory, course.Settings.DictionaryFile);
			return File.Exists(file) ? file : null;
		}
	}
}