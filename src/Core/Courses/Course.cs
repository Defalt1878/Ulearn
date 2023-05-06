using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Units;

namespace Ulearn.Core.Courses
{
	public class Course : ICourse
	{
		public Course(string id, List<Unit> units, [NotNull] CourseSettings settings, [NotNull] CourseVersionToken courseVersionToken)
		{
			Id = id;
			this.units = units;
			Settings = settings;
			CourseVersionToken = courseVersionToken;
		}

		public string Id { get; set; }

		[NotNull]
		public CourseVersionToken CourseVersionToken { get; set; }

		public bool IsTempCourse() => CourseVersionToken.IsTempCourse();

		public string Title => Settings.Title;

		[NotNull]
		public CourseSettings Settings { get; }

		private readonly List<Unit> units;

		private List<Slide> SlidesCache { get; set; }

		private List<Slide> Slides
		{
			get { return SlidesCache ??= units.SelectMany(u => u.GetSlides(true)).ToList(); }
		}

		public List<Slide> GetSlidesNotSafe()
		{
			return Slides;
		}

		// visibleUnits может быть null, если withHidden true
		public List<Slide> GetSlides(bool withHidden, IEnumerable<Guid> visibleUnits)
		{
			if (withHidden)
				return Slides;
			if (!withHidden && visibleUnits == null)
				throw new Exception($"{nameof(GetSlides)} !withHidden && visibleUnits == null");
			return GetNotHiddenSlides(visibleUnits).ToList();
		}

		public Slide FindSlideByIdNotSafe(Guid slideId)
		{
			return FindSlideById(slideId, true, null);
		}

		// visibleUnits может быть null, если withHidden true
		[CanBeNull]
		public Slide FindSlideById(Guid slideId, bool withHidden, IEnumerable<Guid> visibleUnits)
		{
			if (!withHidden && visibleUnits == null)
				throw new Exception($"{nameof(FindSlideById)} !withHidden && visibleUnits == null");
			return (withHidden ? Slides : GetNotHiddenSlides(visibleUnits)).FirstOrDefault(x => x.Id == slideId);
		}

		public Slide GetSlideByIdNotSafe(Guid slideId)
		{
			return GetSlideById(slideId, true, null);
		}

		// visibleUnits может быть null, если withHidden true
		[NotNull]
		public Slide GetSlideById(Guid slideId, bool withHidden, IEnumerable<Guid> visibleUnits)
		{
			var slide = FindSlideById(slideId, withHidden, visibleUnits);
			if (slide == null)
				throw new NotFoundException($"No slide with id {slideId}");
			return slide;
		}

		[CanBeNull]
		public Slide FindInstructorNoteByIdNotSafe(Guid slideId)
		{
			var unitWithId = FindUnitByIdNotSafe(slideId);
			return unitWithId?.InstructorNote;
		}

		[CanBeNull]
		public Unit FindUnitById(Guid unitId, List<Guid> visibleUnits)
		{
			return visibleUnits.Contains(unitId) ? FindUnitByIdNotSafe(unitId) : null;
		}

		[NotNull]
		public Unit GetUnitById(Guid unitId, List<Guid> visibleUnits)
		{
			var unit = FindUnitById(unitId, visibleUnits);
			if (unit == null)
				throw new NotFoundException($"No unit with id {unitId}");
			return unit;
		}

		[CanBeNull]
		public Unit FindUnitByIdNotSafe(Guid unitId)
		{
			return units.FirstOrDefault(x => x.Id == unitId);
		}

		[NotNull]
		public Unit GetUnitByIdNotSafe(Guid unitId)
		{
			var unit = FindUnitByIdNotSafe(unitId);
			if (unit == null)
				throw new NotFoundException($"No unit with id {unitId}");
			return unit;
		}

		public List<Unit> GetUnits(IEnumerable<Guid> visibleUnits)
		{
			var visibleUnitsSet = visibleUnits is HashSet<Guid> ? visibleUnits : visibleUnits.ToHashSet();
			return units.Where(u => visibleUnitsSet.Contains(u.Id)).ToList();
		}

		// Возвращает все юниты курса без проверки, что у пользователя есть доступ к ним
		public List<Unit> GetUnitsNotSafe()
		{
			return units;
		}

		[CanBeNull]
		public Unit FindUnitBySlideIdNotSafe(Guid slideId, bool withHiddenSlides) // не проверяет, что unit опубликован
		{
			return units.FirstOrDefault(u => u.GetSlides(withHiddenSlides).Any(s => s.Id == slideId));
		}

		private IEnumerable<Slide> GetNotHiddenSlides(IEnumerable<Guid> visibleUnits)
		{
			return GetUnits(visibleUnits).SelectMany(u => u.GetSlides(false));
		}

		public override string ToString()
		{
			return $"Course(Id: {Id}, Title: {Title})";
		}
	}
}