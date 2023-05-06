using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Flashcards;
using Ulearn.Core.Courses.Units;
using Ulearn.Core.Extensions;

namespace Ulearn.Core.Courses
{
	public class CourseLoader : ICourseLoader
	{
		public const char CourseIdDelimiter = '@'; // После @ в название могут добавляться доп. символы для временных папок
		private readonly IUnitLoader unitLoader;

		public CourseLoader(IUnitLoader unitLoader)
		{
			this.unitLoader = unitLoader;
		}

		/* For backward compatibility. In future we should use only DI */
		public CourseLoader()
			: this(new UnitLoader(new XmlSlideLoader()))
		{
		}

		public Course Load(DirectoryInfo dir, string courseId)
		{
			try
			{
				return UnsafeLoad(dir, courseId);
			}
			catch (Exception e) when (!e.GetType().IsAssignableFrom(typeof(CourseLoadingException)))
			{
				throw new CourseLoadingException("Не удалось загрузить курс. ", e);
			}
		}

		private Course UnsafeLoad(DirectoryInfo courseDirectory, string courseId)
		{
			CourseSettings settings;
			try
			{
				settings = CourseSettings.Load(courseDirectory);
			}
			catch (Exception e)
			{
				throw new CourseLoadingException($"Не удалось прочитать настройки курса из файла course.xml. {e.Message}", e.InnerException);
			}

			if (string.IsNullOrEmpty(settings.Title))
				try
				{
					settings.Title = GetCourseTitleFromFile(courseDirectory);
				}
				catch (Exception e)
				{
					throw new CourseLoadingException(
						$"Не удалось прочитать настройки курса. Скорее всего, отсутствует или неправильно заполнен файл course.xml. {e.Message}", e.InnerException
					);
				}

			if (string.IsNullOrEmpty(settings.Description))
				settings.Description = "";

			var versionToken = CourseVersionToken.Load(courseDirectory);

			var context = new CourseLoadingContext(courseId, settings, courseDirectory);

			var units = LoadUnits(context).ToList();
			var slides = units.SelectMany(u => u.GetSlides(true)).ToList();

			var validationLog = GetValidationLog(units);
			if (validationLog != string.Empty)
				throw new CourseLoadingException(validationLog);

			AddDefaultScoringGroupIfNeeded(units, slides, settings);
			CalculateScoringGroupScores(units, settings);

			return new Course(courseId, units, settings, versionToken);
		}

		private static string GetValidationLog(List<Unit> units)
		{
			var slides = units.SelectMany(x => x.GetSlides(true)).ToList();
			var validationLog = new List<string>();
			validationLog.Add(CheckEmptySlideIds(slides));
			validationLog.Add(CheckDuplicateSlideIds(slides));
			validationLog.Add(CheckFlashcards(units.Where(x => x.Flashcards.Count > 0).ToList()));
			return validationLog.All(x => x == string.Empty)
				? string.Empty
				: string.Join("\n", validationLog.Where(x => x != string.Empty));
		}

		private static void CalculateScoringGroupScores(IEnumerable<Unit> units, CourseSettings settings)
		{
			foreach (var unit in units)
			foreach (var slide in unit.GetSlides(false).Where(s => s.ShouldBeSolved))
			{
				unit.Scoring.Groups[slide.ScoringGroup].MaxNotAdditionalScore += slide.MaxScore;
				settings.Scoring.Groups[slide.ScoringGroup].MaxNotAdditionalScore += slide.MaxScore;
			}
		}

		private static void AddDefaultScoringGroupIfNeeded(IEnumerable<Unit> units, IEnumerable<Slide> slides, CourseSettings settings)
		{
			if (slides.Any(s => s.ShouldBeSolved && string.IsNullOrEmpty(s.ScoringGroup)))
			{
				var defaultScoringGroup = new ScoringGroup
				{
					Id = "",
					Abbreviation = "Баллы",
					Name = "Упражнения и тесты"
				};
				settings.Scoring.Groups.Add(defaultScoringGroup.Id, defaultScoringGroup);

				/* Add default scoring group to each unit */
				foreach (var unit in units)
					unit.Scoring.Groups.Add(defaultScoringGroup.Id, defaultScoringGroup);
			}
		}


		private IEnumerable<Unit> LoadUnits(CourseLoadingContext context)
		{
			var unitFiles = context.CourseSettings
				.UnitPaths
				.SelectMany(path => context.CourseDirectory.GetFilesByMask(path).OrderBy(f => f.FullName, StringComparer.InvariantCultureIgnoreCase))
				.Distinct()
				/* Don't load unit from course file! Even accidentally */
				.Where(f => !f.Name.Equals("course.xml", StringComparison.OrdinalIgnoreCase))
				.ToList();

			var unitIds = new HashSet<Guid>();
			var unitUrls = new HashSet<string>();
			foreach (var unitFile in unitFiles)
			{
				var unit = unitLoader.Load(unitFile, context);

				if (unitIds.Contains(unit.Id))
					throw new CourseLoadingException($"Ошибка в курсе \"{context.CourseSettings.Title}\" при загрузке модуля \"{unit.Title}\" из {unitFile.FullName}. " +
													$"Повторяющийся идентификатор модуля: {unit.Id}. Идентификаторы модулей должны быть уникальными. " +
													$"К этому времени загружены модули {string.Join(", ", unitIds)}");
				unitIds.Add(unit.Id);

				if (unitUrls.Contains(unit.Url))
					throw new CourseLoadingException(
						$"Ошибка в курсе \"{context.CourseSettings.Title}\" при загрузке модуля \"{unit.Title}\" из {unitFile.FullName}. " +
						$"Повторяющийся url-адрес модуля: {unit.Url}. Url-адреса модулей должны быть уникальными"
					);
				unitUrls.Add(unit.Url);
				if (unit.GetSlides(true).OfType<FlashcardSlide>().Count() > 1)
					throw new CourseLoadingException($"Ошибка в курсе \"{context.CourseSettings.Title}\" при загрузке модуля \"{unit.Title}\" из {unitFile.FullName}. " +
													$"Обнаружено более одного слайда с флеш-картами. Слайд с флеш-картами должен быть только один");

				yield return unit;
			}
		}

		private static string GetCourseTitleFromFile(DirectoryInfo dir)
		{
			return dir.GetFile("Title.txt").ContentAsUtf8();
		}

		private static string CheckDuplicateSlideIds(IEnumerable<Slide> slides)
		{
			var badSlides =
				slides.GroupBy(x => x.Id)
					.Where(x => x.Count() != 1)
					.Select(x => x.Select(y => y.Title))
					.ToList();
			if (badSlides.Any())
				return
					"Идентификаторы слайдов (SlideId) должны быть уникальными.\n" +
					"Слайды с повторяющимися идентификаторами:\n	" +
					string.Join("\n	", badSlides.Select(x => string.Join("\n	", x)));
			return "";
		}

		private static string CheckEmptySlideIds(IEnumerable<Slide> slides)
		{
			var emptyIdSlides = slides.Where(x => x.Id == Guid.Empty).Select(x => x.Title).ToList();
			if (emptyIdSlides.Any())
				return "Идентификаторы слайдов (SlideId) должны быть заполненными.\n" +
						"Слайды с пустыми идентификаторами:\n	" +
						string.Join("\n	", emptyIdSlides.Select(x => string.Join("\n", x)));

			return string.Empty;
		}

		private static string CheckFlashcards(List<Unit> units)
		{
			return CheckDuplicateFlashcardIds(units);
		}


		private static string CheckDuplicateFlashcardIds(List<Unit> units)
		{
			var unitsContainsFlashcardsId = new Dictionary<string, List<string>>();
			foreach (var unit in units)
			foreach (var unitFlashcard in unit.Flashcards)
				if (unitsContainsFlashcardsId.TryGetValue(unitFlashcard.Id, out var unitsTitles))
					unitsTitles.Add(unit.Title);
				else
					unitsContainsFlashcardsId[unitFlashcard.Id] = new List<string> { unit.Title };

			var badFlashcards =
				units.SelectMany(x => x.Flashcards)
					.Where(x => unitsContainsFlashcardsId[x.Id].Count > 1).Select(x => x.Id).Distinct().ToList();
			if (badFlashcards.Any())
				return
					"Идентификаторы флеш-карт (FlashcardId) должны быть уникальными.\n" +
					"Флеш-карты с повторяющимися идентификаторами:\n" +
					string.Join(
						"\n",
						badFlashcards.Select(x => "	Флеш-карта с ID " + x + " в темах:\n		" + string.Join(",\n		", unitsContainsFlashcardsId[x]))
					);
			return string.Empty;
		}
	}

	public class CourseLoadingException : Exception
	{
		public CourseLoadingException(string message)
			: base(message)
		{
		}

		public CourseLoadingException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}