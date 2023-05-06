using System;
using System.IO;
using System.Linq;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Flashcards;

namespace Ulearn.Core.Courses.Units
{
	public class UnitLoader : IUnitLoader
	{
		private readonly ISlideLoader slideLoader;

		public UnitLoader(ISlideLoader slideLoader)
		{
			this.slideLoader = slideLoader;
		}

		public Unit Load(FileInfo unitFile, CourseLoadingContext context)
		{
			var unitDirectory = unitFile.Directory;
			if (unitDirectory == null)
				throw new CourseLoadingException($"Не могу загрузить модуль из {unitFile.GetRelativePath(context.CourseDirectory)}: не могу определить, из какой папки читать файлы со слайдами.");

			UnitSettings unitSettings;
			if (unitFile.Exists)
				unitSettings = UnitSettings.Load(unitFile, context.CourseSettings);
			else
				try
				{
					unitSettings = UnitSettings.CreateByTitle(GetUnitTitleFromFile(unitDirectory), context.CourseSettings);
				}
				catch (Exception e)
				{
					throw new CourseLoadingException(
						$"Не удалось прочитать настройки курса. Скорее всего, отсутствует или неправильно заполнен файл модуля {unitFile.Name} ({unitFile.GetRelativePath(context.CourseDirectory)})."
					);
				}

			var unit = new Unit(unitSettings, unitDirectory.GetRelativePath(context.CourseDirectory));

			var slideFiles = unitSettings
				.SlidesPaths
				.SelectMany(path => unitDirectory.GetFiles(path, SearchOption.TopDirectoryOnly).OrderBy(f => f.FullName, StringComparer.InvariantCultureIgnoreCase))
				.Distinct()
				/* Don't load slide from unit file! Even accidentally */
				.Where(f => f != unitFile);

			unit.SetSlides(slideFiles
				.Select(f => LoadSlide(f, unit, context))
				.ToList());

			var flashcardSlides = unit.GetSlides(true).OfType<FlashcardSlide>();
			foreach (var flashcardSlide in flashcardSlides)
			foreach (var flashcard in flashcardSlide.FlashcardsList)
				unit.Flashcards.Add(flashcard);

			LoadInstructorNote(unit, unitDirectory, context);

			return unit;
		}

		private Slide LoadSlide(FileInfo file, Unit unit, CourseLoadingContext context)
		{
			var slideLoadingContext = new SlideLoadingContext(context, unit, file);
			try
			{
				return slideLoader.Load(slideLoadingContext);
			}
			catch (Exception e)
			{
				if (e.GetType().IsSubclassOf(typeof(CourseLoadingException)))
					throw;
				throw new CourseLoadingException($"Не могу загрузить слайд из файла {file.GetRelativePath(context.CourseDirectory)}", e);
			}
		}

		private static void LoadInstructorNote(Unit unit, DirectoryInfo unitDirectory, CourseLoadingContext context)
		{
			var instructorNoteFile = unitDirectory.GetFile("InstructorNotes.md");
			if (instructorNoteFile.Exists)
				unit.InstructorNote = InstructorNoteLoader.Load(new SlideLoadingContext(context, unit, instructorNoteFile));
		}

		private static string GetUnitTitleFromFile(DirectoryInfo dir)
		{
			return dir.GetFile("Title.txt").ContentAsUtf8();
		}
	}
}