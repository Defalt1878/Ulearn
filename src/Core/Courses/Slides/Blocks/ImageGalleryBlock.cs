using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Ulearn.Common.Extensions;
using Ulearn.Core.Extensions;
using Ulearn.Core.Model.Edx.EdxComponents;

namespace Ulearn.Core.Courses.Slides.Blocks
{
	[XmlType("galleryImages")]
	public class ImageGalleryBlock : SlideBlock, IConvertibleToEdx
	{
		[XmlElement("image")]
		public string[] RelativeToUnitDirectoryImagePaths { get; set; }

		public ImageGalleryBlock(string[] relativeToUnitDirectoryImagePaths)
		{
			RelativeToUnitDirectoryImagePaths = relativeToUnitDirectoryImagePaths;
		}

		public IEnumerable<string> GetAbsoluteImageUrls(string baseUrlApi, string courseId, string unitPathRelativeToCourse)
		{
			return RelativeToUnitDirectoryImagePaths.Select(p => CourseUrlHelper.GetAbsoluteUrlToFile(baseUrlApi, courseId, unitPathRelativeToCourse, p));
		}

		public ImageGalleryBlock()
		{
		}

		public override IEnumerable<SlideBlock> BuildUp(SlideBuildingContext context, IImmutableSet<string> filesInProgress)
		{
			RelativeToUnitDirectoryImagePaths = RelativeToUnitDirectoryImagePaths
				.SelectMany(path => context.UnitDirectory.GetFilesByMask(path).OrderBy(f => f.FullName, StringComparer.InvariantCultureIgnoreCase))
				.Select(file => file.GetRelativePath(context.UnitDirectory))
				.ToArray();
			yield return this;
		}

		public override string ToString()
		{
			return $"Gallery with images:\n{string.Join("\n", RelativeToUnitDirectoryImagePaths)}";
		}

		public Component ToEdxComponent(EdxComponentBuilderContext context)
		{
			var urlName = context.Slide.NormalizedGuid + context.ComponentIndex;
			var imageFiles = RelativeToUnitDirectoryImagePaths
				.Select(p => (
					ImageFile: new FileInfo(Path.Combine(context.CourseDirectory.FullName, context.Slide.Unit.UnitDirectoryRelativeToCourse, p)),
					RelativeToUnitDirectoryImagePath: p.Replace('\\', '/')
				))
				.ToArray();
			return new GalleryComponent(urlName, context.DisplayName, urlName, imageFiles);
		}
	}
}