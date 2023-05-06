using System;
using System.Collections.Generic;
using System.Linq;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Flashcards;

namespace Ulearn.Core.Courses.Units
{
	public class Unit
	{
		public Unit(UnitSettings settings, string unitDirectoryRelativeToCourse)
		{
			Settings = settings;
			UnitDirectoryRelativeToCourse = unitDirectoryRelativeToCourse;
			Flashcards = new List<Flashcard>();
		}

		public UnitSettings Settings { get; set; }

		public Slide InstructorNote { get; set; }

		private List<Slide> Slides { get; set; }

		private List<Slide> notHiddenSlides { get; set; }

		private List<Slide> NotHiddenSlides
		{
			get { return notHiddenSlides ??= Slides.Where(s => !s.Hide).ToList(); }
		}

		public List<Flashcard> Flashcards { get; set; }

		public string UnitDirectoryRelativeToCourse { get; }

		public Guid Id => Settings.Id;

		public string Title => Settings.Title;

		public string Url => Settings.Url;

		public ScoringSettings Scoring => Settings.Scoring;

		// Используется только в UnitLoader. Иначе в Course не обновится slidesCache
		public void SetSlides(List<Slide> slides)
		{
			Slides = slides;
			notHiddenSlides = null;
		}

		public List<Slide> GetSlides(bool withHidden)
		{
			return withHidden ? Slides : NotHiddenSlides;
		}

		public List<Slide> GetHiddenSlides()
		{
			return Slides.Where(s => s.Hide).ToList();
		}

		public Flashcard GetFlashcardById(string id)
		{
			return Flashcards.FirstOrDefault(x => x.Id == id);
		}
	}
}