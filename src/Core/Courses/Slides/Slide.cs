using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Slides.Blocks;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Courses.Slides.Exercises.Blocks;
using Ulearn.Core.Courses.Slides.Flashcards;
using Ulearn.Core.Courses.Slides.Quizzes;
using Ulearn.Core.Courses.Slides.Quizzes.Blocks;
using Ulearn.Core.Courses.Units;
using Ulearn.Core.Extensions;
using Ulearn.Core.Model.Edx;
using Ulearn.Core.Model.Edx.EdxComponents;
using Component = Ulearn.Core.Model.Edx.EdxComponents.Component;

namespace Ulearn.Core.Courses.Slides
{
	[XmlRoot("slide", IsNullable = false, Namespace = "https://ulearn.me/schema/v2")]
	public class Slide : ISlide
	{
		[XmlIgnore]
		public BlockType[] DefineBlockType;

		public Slide()
		{
		}

		public Slide(params SlideBlock[] blocks)
		{
			Blocks = blocks;
		}

		[XmlIgnore]
		public string NormalizedGuid => Id.GetNormalizedGuid();

		[XmlElement("meta")]
		public SlideMetaDescription Meta { get; set; }

		[XmlAttribute("hide")]
		[DefaultValue(false)]
		public bool Hide { get; set; }

		[XmlAttribute("isExtraContent")]
		[DefaultValue(false)]
		public bool IsExtraContent { get; set; }

		[XmlElement("defaultIncludeCodeFile")]
		public string DefaultIncludeCodeFile { get; set; }

		/* This property is extended by QuizSlide and ExerciseSlide */
		[XmlIgnore]
		protected virtual Type[] AllowedBlockTypes { get; } =
		{
			typeof(YoutubeBlock),
			typeof(MarkdownBlock),
			typeof(CodeBlock),
			typeof(TexBlock),
			typeof(ImageGalleryBlock),
			typeof(IncludeCodeBlock),
			typeof(IncludeMarkdownBlock),
			typeof(IncludeImageGalleryBlock),
			typeof(HtmlBlock),
			typeof(SelfCheckupsBlock),
			typeof(SpoilerBlock)
		};

		[XmlIgnore]
		public Unit Unit { get; set; }

		[XmlIgnore]
		public string SlideFilePathRelativeToCourse { get; set; }

		public virtual bool ShouldBeSolved => false;

		public virtual int MaxScore => 0;

		[XmlIgnore]
		public string LatinTitle => Title.ToLatin();

		[XmlIgnore]
		public string Url => LatinTitle + "_" + NormalizedGuid;

		[NotNull]
		[XmlIgnore]
		public virtual string ScoringGroup { get; protected set; } = "";

		[XmlAttribute("id")]
		public Guid Id { get; set; }

		[XmlAttribute("title")]
		public string Title { get; set; }

		/* If you add new block type, don't forget to:
		* 1. Add it to AllowedBlockTypes of Slide, QuizSlide or ExerciseSlide
		* 2. If it's a common block, add it inside of SpoilerBlock tags too
		* 3. Add definition for new block type in BlockType.cs
		*/

		/* Common blocks */
		[XmlElement(typeof(YoutubeBlock))]
		[XmlElement("markdown", typeof(MarkdownBlock))]
		[XmlElement(typeof(CodeBlock))]
		[XmlElement(typeof(TexBlock))]
		[XmlElement(typeof(ImageGalleryBlock))]
		[XmlElement(typeof(IncludeCodeBlock))]
		[XmlElement(typeof(IncludeMarkdownBlock))]
		[XmlElement(typeof(IncludeImageGalleryBlock))]
		[XmlElement("html", typeof(HtmlBlock))]
		[XmlElement(typeof(SpoilerBlock))]
		[XmlElement(typeof(SelfCheckupsBlock))]
		/* Quiz blocks */
		[XmlElement(typeof(IsTrueBlock))]
		[XmlElement(typeof(ChoiceBlock))]
		[XmlElement(typeof(FillInBlock))]
		[XmlElement(typeof(OrderingBlock))]
		[XmlElement(typeof(MatchingBlock))]
		/* Exercise blocks */
		[XmlElement(typeof(CsProjectExerciseBlock))]
		[XmlElement(typeof(SingleFileExerciseBlock))]
		[XmlElement(typeof(UniversalExerciseBlock))]
		[XmlElement(typeof(PolygonExerciseBlock))]
		[XmlChoiceIdentifier(nameof(DefineBlockType))]
		public SlideBlock[] Blocks { get; set; }

		/// <summary>
		///     Validate slide. We guarantee that Validate() will be called after BuildUp()
		/// </summary>
		public virtual void Validate(SlideLoadingContext context)
		{
			var slideLoadingContext = new SlideBuildingContext(context, this);
			foreach (var block in Blocks)
				block.Validate(slideLoadingContext);
		}

		/// <summary>
		///     Building slide and blocks, fill properties, initialize some values. Any work we need to do before work with slide.
		/// </summary>
		public virtual void BuildUp(SlideLoadingContext context)
		{
			Unit = context.Unit;
			SlideFilePathRelativeToCourse = context.SlideFilePathRelativeToCourse;
			Blocks ??= Array.Empty<SlideBlock>();

			/* Validate block types. We should do it before building up blocks */
			CheckBlockTypes();

			/* ... and build blocks */
			var slideLoadingContext = new SlideBuildingContext(context, this);
			Blocks = Blocks.SelectMany(b => b.BuildUp(slideLoadingContext, ImmutableHashSet<string>.Empty)).ToArray();

			DefineBlockTypes();
		}

		public void DefineBlockTypes()
		{
			if (Blocks != null)
				DefineBlockType = Blocks.Select(BlockTypeHelpers.GetBlockType).ToArray();
		}

		public void CheckBlockTypes()
		{
			foreach (var block in Blocks)
				if (!AllowedBlockTypes.Any(type => type.IsInstanceOfType(block)))
					throw new CourseLoadingException(
						$"Недопустимый тип блока в слайде {SlideFilePathRelativeToCourse}: <{block.GetType().GetXmlType()}>. " +
						$"В этом слайде разрешены только следующие блоки: {string.Join(", ", AllowedBlockTypes.Select(t => $"<{t.GetXmlType()}>"))}"
					);
		}

		public AbstractQuestionBlock FindBlockById(string id)
		{
			return Blocks.OfType<AbstractQuestionBlock>().FirstOrDefault(block => block.Id == id);
		}

		public override string ToString()
		{
			return $"Title: {Title}, Id: {NormalizedGuid}, MaxScore: {MaxScore}";
		}

		public IEnumerable<SlideBlock[]> GetBlockRangesWithSameVisibility(bool startNewRangeFromExerciseBlock)
		{
			if (Blocks.Length == 0)
				yield break;
			var range = new List<SlideBlock> { Blocks[0] };
			foreach (var block in Blocks.Skip(1))
			{
				if (block.Hide != range.Last().Hide || (startNewRangeFromExerciseBlock && block is AbstractExerciseBlock))
				{
					yield return range.ToArray();
					range.Clear();
				}

				range.Add(block);
			}

			yield return range.ToArray();
		}

		#region ExportToEdx

		private static IEnumerable<Vertical> OrdinarySlideToVerticals(EdxComponentBuilderContext context, string ltiId)
		{
			var slide = context.Slide;
			var componentIndex = 0;
			var components = new List<Component>();

			var visibleSlideBlocks = slide.Blocks.Where(b => !b.Hide).ToArray();
			while (visibleSlideBlocks.Any(b => b is SpoilerBlock))
				visibleSlideBlocks = visibleSlideBlocks.SelectMany(block =>
				{
					if (block is SpoilerBlock sb)
						return sb.Blocks.Where(b => !b.Hide);
					return new[] { block };
				}).ToArray();

			var staticFilesFromMarkdown = new List<StaticFileForEdx>();
			visibleSlideBlocks = visibleSlideBlocks.SelectMany(b =>
			{
				if (b is MarkdownBlock mb)
				{
					var (blocks, staticFiles)
						= mb.ToHtmlAndCodeBlocks(context.UlearnBaseUrlApi, context.UlearnBaseUrlWeb, context.CourseId, context.Slide, context.CourseDirectory);
					staticFilesFromMarkdown.AddRange(staticFiles);
					return blocks;
				}

				return new List<SlideBlock> { b };
			}).ToArray();

			while (componentIndex < visibleSlideBlocks.Length)
			{
				// Соседние блоки, кроме YoutubeBlock и AbstractExerciseBlock, склеиваются в один HtmlComponent
				var blocks = visibleSlideBlocks.Skip(componentIndex).TakeWhile(x => !(x is YoutubeBlock) && !(x is AbstractExerciseBlock)).ToList();
				if (blocks.Count != 0)
				{
					var innerComponents = new List<Component>();
					foreach (var block in blocks)
					{
						if (block is IConvertibleToEdx convertibleBlock)
						{
							var component = convertibleBlock.ToEdxComponent(context with { ComponentIndex = componentIndex });
							innerComponents.Add(component);
						}
						else
						{
							Console.WriteLine($@"Slide {slide.Id} {block.GetType().Name} block NotSupportedException");
						}

						componentIndex++;
					}

					if (innerComponents.Any())
					{
						componentIndex--; // Иначе компонент будет иметь индекс на один больший, и потом следующий компонент будет с тем же индексом. В конце if возвращаю.
						var displayName = componentIndex == blocks.Count - 1 ? slide.Title : "";
						var header = displayName == "" ? "" : "<h2>" + displayName + "</h2>";
						var slideComponent = new HtmlComponent
						{
							DisplayName = displayName,
							UrlName = slide.NormalizedGuid + componentIndex, // Поскольку склеиваем несколько компонент в html
							Filename = slide.NormalizedGuid + componentIndex,
							HtmlContent = header + string.Join("", innerComponents.Select(x => x.AsHtmlString())),
							Subcomponents = innerComponents.ToArray(),
							StaticFiles = staticFilesFromMarkdown
						};
						staticFilesFromMarkdown = null; // Сохраняем один раз
						components.Add(slideComponent);
						componentIndex++;
					}
				}

				if (componentIndex >= visibleSlideBlocks.Length)
					break;

				if (visibleSlideBlocks[componentIndex] is AbstractExerciseBlock)
				{
					var exerciseComponent = ((ExerciseSlide)slide).GetExerciseComponent(componentIndex == 0 ? slide.Title : "Упражнение", slide, componentIndex, string.Format(context.UlearnBaseUrlWeb + SlideUrlFormat, context.CourseId, slide.Id), ltiId);
					components.Add(exerciseComponent);
				}
				else if (visibleSlideBlocks[componentIndex] is YoutubeBlock youtubeBlock)
				{
					var videoComponent = youtubeBlock.ToEdxComponent(context with { ComponentIndex = componentIndex, DisplayName = componentIndex == 0 ? slide.Title : "" });
					components.Add(videoComponent);
				}

				componentIndex++;
			}

			var exBlock = visibleSlideBlocks.OfType<AbstractExerciseBlock>().FirstOrDefault();
			if (exBlock == null)
			{
				yield return new Vertical(slide.NormalizedGuid, slide.Title, components.ToArray());
			}
			else
			{
				var exerciseSlide = (ExerciseSlide)slide;
				yield return new Vertical(slide.NormalizedGuid, slide.Title, components.ToArray(), EdxScoringGroupsHack.ToEdxName(exerciseSlide.ScoringGroup), exerciseSlide.MaxScore);
			}
		}

		private static IEnumerable<Vertical> QuizToVerticals(string courseId, QuizSlide slide, string slideUrl, string ltiId)
		{
			var ltiComponent =
				new LtiComponent(slide.Title, slide.NormalizedGuid + "-quiz", string.Format(slideUrl, courseId, slide.Id), ltiId, true, slide.MaxScore, false);
			yield return new Vertical(slide.NormalizedGuid, slide.Title, new Component[] { ltiComponent }, EdxScoringGroupsHack.ToEdxName(slide.ScoringGroup), slide.MaxScore);
		}

		protected const string SlideUrlFormat = "/Course/{0}/LtiSlide?slideId={1}";

		public IEnumerable<Vertical> ToVerticals(string courseId, string ulearnBaseUrlApi, string ulearnBaseUrlWeb, string ltiId, DirectoryInfo courseDirectory)
		{
			var slideUrl = ulearnBaseUrlWeb + SlideUrlFormat;
			if (this is QuizSlide quizSlide)
				return QuizToVerticals(courseId, quizSlide, slideUrl, ltiId).ToList();
			if (this is FlashcardSlide)
				return Enumerable.Empty<Vertical>();
			return OrdinarySlideToVerticals(new EdxComponentBuilderContext("", courseId, this, 0, ulearnBaseUrlApi, ulearnBaseUrlWeb, courseDirectory), ltiId).ToList();
		}

		#endregion
	}

	public record EdxComponentBuilderContext(string DisplayName, string CourseId, Slide Slide, int ComponentIndex,
		string UlearnBaseUrlApi, string UlearnBaseUrlWeb, DirectoryInfo CourseDirectory);

	[JsonConverter(typeof(StringEnumConverter), true)]
	public enum SlideType
	{
		[XmlEnum("lesson")]
		Lesson,

		[XmlEnum("quiz")]
		Quiz,

		[XmlEnum("exercise")]
		Exercise,

		[XmlEnum("flashcards")]
		Flashcards
	}

	internal class EdxScoringGroupsHack
	{
		public static string ToEdxName(string scoringGroup)
		{
			return scoringGroup == "homework" ? "Практика" : "Упражнения";
		}
	}
}