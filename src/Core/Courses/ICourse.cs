using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Units;

namespace Ulearn.Core.Courses
{
	public interface ICourse
	{
		string Id { get; set; }
		string Title { get; }
		CourseSettings Settings { get; }
		List<Unit> GetUnits([NotNull] IEnumerable<Guid> visibleUnits);
		List<Unit> GetUnitsNotSafe();

		List<Slide> GetSlidesNotSafe();
		List<Slide> GetSlides(bool withHidden, [CanBeNull] IEnumerable<Guid> visibleUnits);
		Slide FindSlideById(Guid slideId, bool withHidden, [CanBeNull] IEnumerable<Guid> visibleUnits);
		Slide FindSlideByIdNotSafe(Guid slideId);
		Slide GetSlideById(Guid slideId, bool withHidden, [CanBeNull] IEnumerable<Guid> visibleUnits);
		Slide GetSlideByIdNotSafe(Guid slideId);
		Unit FindUnitById(Guid unitId, [NotNull] List<Guid> visibleUnits);
		Unit GetUnitById(Guid unitId, [NotNull] List<Guid> visibleUnits);
		Unit FindUnitByIdNotSafe(Guid unitId);
		Unit GetUnitByIdNotSafe(Guid unitId);
		Unit FindUnitBySlideIdNotSafe(Guid slideId, bool withHidden);
		Slide FindInstructorNoteByIdNotSafe(Guid slideId);
	}
}