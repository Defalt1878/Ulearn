using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Xml.Serialization;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Slides.Blocks;
using Ulearn.Core.Model;
using Ulearn.Core.RunCheckerJobApi;

namespace Ulearn.Core.Courses.Slides.Exercises.Blocks
{
	[XmlType("exercise.file")]
	public class SingleFileExerciseBlock : AbstractExerciseBlock
	{
		[XmlElement("solution")]
		public Label SolutionLabel { get; set; }

		[XmlElement("remove")]
		public Label[] RemovedLabels { get; set; }

		[XmlElement("prelude")]
		public string PreludeFile { get; set; }

		[XmlAttribute("file")]
		public string CodeFile { get; set; }

		// То, что будет выполняться для проверки задания
		[XmlIgnore]
		public string ExerciseCode { get; set; }

		// Индекс внутри ExerciseCode, куда нужно вставить код пользователя.
		[XmlIgnore]
		public int IndexToInsertSolution { get; set; }

		// Если это вставить в ExerciseCode по индексу IndexToInsertSolution и выполнить полученный код, он должен вывести ExpectedOutput
		[XmlIgnore]
		public string EthalonSolution { get; set; }

		// Временно создано для конвертации .cs-слайдов в .xml-слайды. Когда .cs-слайдов не останется, можно удалить
		[XmlIgnore]
		public List<string> ExcludedFromSolution { get; set; } = new();

		public override bool HasAutomaticChecking()
		{
			return Language == Common.Language.CSharp;
		}

		public override IEnumerable<SlideBlock> BuildUp(SlideBuildingContext context, IImmutableSet<string> filesInProgress)
		{
			CodeFile ??= context.Slide.DefaultIncludeCodeFile ?? context.Unit.Settings?.DefaultIncludeCodeFile;
			if (CodeFile == null)
				throw new CourseLoadingException("У блока <exercise.file> не указан атрибут file.");

			if (ExerciseInitialCode == null)
				throw new CourseLoadingException("У блока <exercise.file> не указан код, который надо показывать пользователю перед началом работы. Укажите его в тэге <initialCode>");

			Language ??= LanguageHelpers.GuessByExtension(new FileInfo(CodeFile));

			RemovedLabels ??= Array.Empty<Label>();
			if (PreludeFile == null)
			{
				PreludeFile = context.CourseSettings.GetPrelude(Language);
				if (PreludeFile != null)
					PreludeFile = Path.Combine("..", PreludeFile);
			}

			var code = context.UnitDirectory.GetContent(CodeFile);
			var regionRemover = new RegionRemover(Language);
			var extractor = context.GetExtractor(CodeFile, Language, code);

			var prelude = "";
			if (PreludeFile != null)
				prelude = context.UnitDirectory.GetContent(PreludeFile);

			var exerciseCode = regionRemover.Prepare(code);
			exerciseCode = regionRemover.Remove(exerciseCode, RemovedLabels, out var _);
			exerciseCode = regionRemover.RemoveSolution(exerciseCode, SolutionLabel, out var index);
			if (index < 0)
				index = 0;
			index += prelude.Length;

			ExerciseInitialCode = ExerciseInitialCode.RemoveCommonNesting();
			ExerciseCode = prelude + exerciseCode;
			IndexToInsertSolution = index;
			EthalonSolution = extractor.GetRegion(SolutionLabel);
			Validator.ValidatorName = string.Join(" ", Language.GetName(), Validator.ValidatorName ?? "");

			yield return this;
		}

		public override string GetSourceCode(string code)
		{
			return BuildSolution(code).SourceCode;
		}

		public override SolutionBuildResult BuildSolution(string userWrittenCode, Language? language = null)
		{
			return new SolutionBuilder(IndexToInsertSolution, ExerciseCode, Validator).BuildSolution(userWrittenCode);
		}

		public override RunnerSubmission CreateSubmission(string submissionId, string code, string courseDirectory)
		{
			return new FileRunnerSubmission
			{
				Id = submissionId,
				Code = GetSourceCode(code),
				Input = "",
				NeedRun = true,
				TimeLimit = TimeLimit
			};
		}
	}
}