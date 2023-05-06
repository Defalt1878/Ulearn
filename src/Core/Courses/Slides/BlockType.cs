using System;
using System.Xml.Serialization;
using Ulearn.Core.Courses.Slides.Blocks;
using Ulearn.Core.Courses.Slides.Exercises.Blocks;
using Ulearn.Core.Courses.Slides.Quizzes.Blocks;

namespace Ulearn.Core.Courses.Slides
{
	[XmlType(IncludeInSchema = false)]
	public enum BlockType
	{
		[XmlEnum("youtube")]
		YouTube,

		[XmlEnum("markdown")]
		Markdown,

		[XmlEnum("code")]
		Code,

		[XmlEnum("tex")]
		Tex,

		[XmlEnum("galleryImages")]
		GalleryImages,

		[XmlEnum("includeCode")]
		IncludeCode,

		[XmlEnum("includeMarkdown")]
		IncludeMarkdown,

		[XmlEnum("includeBlocks")]
		IncludeBlocks,

		[XmlEnum("gallery")]
		IncludeImageGallery,

		[XmlEnum("html")]
		Html,

		[XmlEnum("selfCheckups")]
		SelfCheckups,

		[XmlEnum("spoiler")]
		Spoiler,

		[XmlEnum("exercise.file")]
		SingleFileExercise,

		[XmlEnum("exercise.csproj")]
		CsProjectExercise,

		[XmlEnum("question.isTrue")]
		IsTrueQuestion,

		[XmlEnum("question.choice")]
		ChoiceQuestion,

		[XmlEnum("question.text")]
		TextQuestion,

		[XmlEnum("question.order")]
		OrderQuestion,

		[XmlEnum("question.match")]
		MatchQuestion,

		[XmlEnum("exercise.universal")]
		UniversalExercise,

		[XmlEnum("exercise.polygon")]
		PolygonExercise
	}

	public static class BlockTypeHelpers
	{
		public static BlockType GetBlockType(SlideBlock block)
		{
			return block switch
			{
				YoutubeBlock _ => BlockType.YouTube,
				CodeBlock _ => BlockType.Code,
				ImageGalleryBlock _ => BlockType.GalleryImages,
				IncludeCodeBlock _ => BlockType.IncludeCode,
				IncludeImageGalleryBlock _ => BlockType.IncludeImageGallery,
				IncludeMarkdownBlock _ => BlockType.IncludeMarkdown,
				MarkdownBlock _ => BlockType.Markdown,
				TexBlock _ => BlockType.Tex,
				HtmlBlock _ => BlockType.Html,
				SpoilerBlock _ => BlockType.Spoiler,
				SelfCheckupsBlock _ => BlockType.SelfCheckups,
				FillInBlock _ => BlockType.TextQuestion,
				ChoiceBlock _ => BlockType.ChoiceQuestion,
				MatchingBlock _ => BlockType.MatchQuestion,
				OrderingBlock _ => BlockType.OrderQuestion,
				IsTrueBlock _ => BlockType.IsTrueQuestion,
				CsProjectExerciseBlock _ => BlockType.CsProjectExercise,
				SingleFileExerciseBlock _ => BlockType.SingleFileExercise,
				UniversalExerciseBlock _ => BlockType.UniversalExercise,
				_ => throw new Exception("Unknown slide block " + block)
			};
		}
	}
}