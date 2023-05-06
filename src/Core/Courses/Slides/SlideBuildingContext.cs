using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Units;
using Ulearn.Core.Model;

namespace Ulearn.Core.Courses.Slides
{
	public class SlideBuildingContext
	{
		public string CourseId { get; private set; }
		public CourseSettings CourseSettings { get; private set; }

		public DirectoryInfo UnitDirectory { get; }
		public DirectoryInfo CourseDirectory { get; }
		public Slide Slide { get; set; }

		public Unit Unit { get; }

		private List<RegionsExtractor> Extractors { get; }

		public SlideBuildingContext(string courseId, Unit unit, CourseSettings courseSettings, DirectoryInfo courseDirectory, DirectoryInfo unitDirectory, Slide slide)
		{
			CourseId = courseId;

			Unit = unit;
			UnitDirectory = unitDirectory;
			CourseDirectory = courseDirectory;
			CourseSettings = courseSettings;
			Slide = slide;
			Extractors = new List<RegionsExtractor>();
		}

		public SlideBuildingContext(SlideLoadingContext slideContext, Slide slide)
			: this(slideContext.CourseId, slideContext.Unit, slideContext.CourseSettings, slideContext.CourseDirectory, slideContext.UnitDirectory, slide)
		{
		}

		public RegionsExtractor GetExtractor(string filename, Language? language, string code = null)
		{
			var extractor = Extractors.FirstOrDefault(regionsExtractor => regionsExtractor.Filename == filename && regionsExtractor.Language == language);
			if (extractor != null)
				return extractor;

			code ??= UnitDirectory.GetContent(filename);
			extractor = new RegionsExtractor(code, language, filename);
			Extractors.Add(extractor);
			return extractor;
		}
	}
}