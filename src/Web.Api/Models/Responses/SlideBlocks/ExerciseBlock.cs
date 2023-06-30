using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Slides.Exercises.Blocks;
using Ulearn.Web.Api.Controllers.Slides;
using Ulearn.Web.Api.Models.Responses.Exercise;
using Ulearn.Core.Markdown;

namespace Ulearn.Web.Api.Models.Responses.SlideBlocks
{
	[DataContract]
	[DisplayName("exercise")]
	public class ExerciseBlockResponse : IApiSlideBlock
	{
		[DefaultValue(false)]
		[DataMember(EmitDefaultValue = false)]
		public bool Hide { get; set; }

		[DataMember]
		public Language[] Languages { get; set; }

		[DataMember]
		public Dictionary<Language, LanguageLaunchInfo>? LanguageInfo { get; set; } // Для языка содержит его текстовое название, если оно не такое же, как поле enum
		
		[DataMember]
		public Language? DefaultLanguage { get; set; }
		

		[DataMember]
		public string[] RenderedHints { get; set; }

		[DataMember]
		public string ExerciseInitialCode { get; set; }

		[DataMember]
		public bool HideSolutions { get; set; }

		[DataMember]
		public string? ExpectedOutput { get; set; } // В том числе может быть не пуст, но скрыт от студента, тогда и здесь null
		
		[DataMember]
		public List<SelfCheckupResponse>? Checkups { get; set; }

		[DataMember]
		public ExerciseAttemptsStatistics AttemptsStatistics { get; set; }
		
		[DataMember]
		public bool PythonVisualizerEnabled { get; set; }
		
		[DataMember]
		public ExerciseTexts ExerciseTexts { get; set; }

		public ExerciseBlockResponse(AbstractExerciseBlock exerciseBlock,
			ExerciseSlideRendererContext context)
		{
			Checkups = context.Checkups;
			ExerciseTexts = exerciseBlock.Texts;
			if (exerciseBlock is PolygonExerciseBlock polygonExerciseBlock)
			{
				Languages = PolygonExerciseBlock.LanguagesInfo.Keys.ToArray();
				LanguageInfo = PolygonExerciseBlock.LanguagesInfo;
				DefaultLanguage = polygonExerciseBlock.DefaultLanguage;
				PythonVisualizerEnabled = polygonExerciseBlock.PythonVisualizerEnabled;
			}
			else
			{
				Languages = exerciseBlock.Language != null ? new[] { exerciseBlock.Language.Value } : new Language[0];
				LanguageInfo = null;
				DefaultLanguage = null;
			}

			RenderedHints = exerciseBlock.Hints.Select(h => RenderHtmlWithHint(h, context.markdownRenderContext)).ToArray();
			ExerciseInitialCode = exerciseBlock.ExerciseInitialCode.RemoveEmptyLinesFromStart().TrimEnd().EnsureEnoughLines(4);
			HideSolutions = exerciseBlock.HideShowSolutionsButton;
			ExpectedOutput = exerciseBlock.HideExpectedOutputOnError ? null : exerciseBlock.ExpectedOutput?.NormalizeEoln();
			AttemptsStatistics = context.AttemptsStatistics;
		}

		private string RenderHtmlWithHint(string hintMd, MarkdownRenderContext markdownRenderContext)
		{
			return hintMd.RenderMarkdown(markdownRenderContext);
		}
	}
}