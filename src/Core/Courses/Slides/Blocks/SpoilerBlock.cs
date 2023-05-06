using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Serialization;
using Ulearn.Core.Courses.Slides.Quizzes;

namespace Ulearn.Core.Courses.Slides.Blocks
{
	[XmlType("spoiler")]
	public class SpoilerBlock : SlideBlock
	{
		[XmlAttribute("text")]
		public string Text { get; set; }

		[XmlAttribute("hideQuizButton")]
		public bool HideQuizButton { get; set; }

		[XmlElement(typeof(YoutubeBlock))]
		[XmlElement("markdown", typeof(MarkdownBlock))]
		[XmlElement(typeof(CodeBlock))]
		[XmlElement(typeof(TexBlock))]
		[XmlElement(typeof(ImageGalleryBlock))]
		[XmlElement(typeof(IncludeCodeBlock))]
		[XmlElement(typeof(IncludeMarkdownBlock))]
		[XmlElement(typeof(IncludeImageGalleryBlock))]
		[XmlElement("html", typeof(HtmlBlock))]
		[XmlElement(typeof(SelfCheckupsBlock))]
		[XmlChoiceIdentifier(nameof(DefineBlockType))]
		public SlideBlock[] Blocks { get; set; }

		[XmlIgnore]
		public BlockType[] DefineBlockType;

		public override string ToString()
		{
			return $"Spoiler {Text}";
		}

		public override IEnumerable<SlideBlock> BuildUp(SlideBuildingContext context, IImmutableSet<string> filesInProgress)
		{
			Blocks ??= Array.Empty<SlideBlock>();

			Blocks = Blocks.SelectMany(b => b.BuildUp(context, filesInProgress)).ToArray();

			DefineBlockTypes();

			return base.BuildUp(context, filesInProgress);
		}

		public void DefineBlockTypes()
		{
			if (Blocks != null)
				DefineBlockType = Blocks.Select(BlockTypeHelpers.GetBlockType).ToArray();
		}

		public override void Validate(SlideBuildingContext slideBuildingContext)
		{
			if (HideQuizButton && slideBuildingContext.Slide is not QuizSlide)
				throw new CourseLoadingException("У блока <spoiler> указан атрибут hideQuizButton=\"true\", хотя слайд не является тестом. Этот атрибут можно использовать только внутри <slide.quiz>");
		}
	}
}