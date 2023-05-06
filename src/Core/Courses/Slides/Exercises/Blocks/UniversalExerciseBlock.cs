using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using JetBrains.Annotations;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides.Blocks;
using Ulearn.Core.Extensions;
using Ulearn.Core.Helpers;
using Ulearn.Core.Model;
using Ulearn.Core.RunCheckerJobApi;
using Vostok.Logging.Abstractions;

namespace Ulearn.Core.Courses.Slides.Exercises.Blocks
{
	[XmlType("exercise.universal")]
	public class UniversalExerciseBlock : AbstractExerciseBlock
	{
		[XmlIgnore]
		public static readonly Regex DockerImageNameRegex = new("^[-_a-z.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		[XmlIgnore]
		public static readonly Regex RunCommandRegex = new("^[-_a-zA-Z.0-9 ;,|&>]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		[XmlIgnore]
		private static readonly string[] wrongAnswerPatterns = { "*.WrongAnswer.*", "*.WA.*" };

		[XmlIgnore]
		private static readonly string[] initialPatterns = { "*.Initial.*", "*.initial.*" };

		[XmlIgnore]
		private static readonly string[] solutionPatterns = { "*.Solution.*" };

		[XmlIgnore]
		public CommonSingleRegionExtractor Extractor;

		[XmlIgnore]
		public Lazy<string> InitialRegionContent;

		[XmlIgnore]
		public Lazy<string> SolutionRegionContent;

		private static ILog Log => LogProvider.Get().ForContext(typeof(UniversalExerciseBlock));

		[XmlAttribute("exerciseDirName")] // Путь до директории с упражнением
		public string ExerciseDirPath { get; set; }

		[XmlElement("userCodeFile")]
		public string UserCodeFilePath { get; set; }

		[XmlElement("solutionFilePath")]
		public string SolutionFilePath { get; set; } // По умолчанию используется UserCodeFilePath

		[XmlElement("region")]
		public virtual string Region { get; set; } // Студент видит в консоли и редактирует регион с этим именем. <prefix>region label <...> <prefix>rendregion label. Должен быть как в решении, так и в файле заглушке.

		[XmlElement("excludePathForChecker")]
		public virtual string[] PathsToExcludeForChecker { get; set; }

		[CanBeNull]
		[XmlElement("includePathForChecker")]
		public string[] PathsToIncludeForChecker { get; set; } // Пути до директорий относительно директории с Unit, чьё содержимое нужно включить в архив. Перетирают файлы из ExerciseDir в архиве

		[CanBeNull]
		[XmlElement("includePathForStudent")]
		public string[] PathsToIncludeForStudent { get; set; }

		[XmlElement("excludePathForStudent")]
		public string[] PathsToExcludeForStudent { get; set; } // Шаблоны путей до файлов внутри ExerciseDir.

		[XmlElement("checkInitialSolution")]
		public virtual bool CheckInitialSolution { get; set; } = true;

		[XmlAttribute("noStudentZip")] // Не отдавать zip студенту
		public virtual bool NoStudentZip { get; set; }


		[XmlElement("interpretNonJsonOutputAs")]
		public virtual InterpretNonJsonOutputType? InterpretNonJsonOutputAs { get; set; }

		[XmlElement("dockerImageName")] // см. DockerImageNameRegex
		public virtual string DockerImageName { get; set; }

		[XmlElement("run")]
		public string RunCommand { get; set; } // см. RunCommandRegex

		[XmlIgnore]
		public string UnitDirectoryPathRelativeToCourse { get; set; }

		[XmlIgnore]
		public string InitialUserCodeFilePath
		{
			get
			{
				var parts = UserCodeFilePath.Split('.');
				return string.Join(".", parts.Take(parts.Length - 1).Concat(new[] { "Initial", parts.Last() }));
			}
		}

		[XmlIgnore]
		public string CorrectSolutionFilePath => SolutionFilePath ?? UserCodeFilePath;

		[XmlIgnore]
		public Slide Slide { get; private set; }

		[XmlIgnore]
		public string CourseId { get; private set; }

		private static string InitialFilePathToNotInitial(string path)
		{
			var notInitial = path;
			initialPatterns.ForEach(p => notInitial = notInitial.Replace(p.Replace(".*", "").Replace("*", ""), ""));
			return notInitial;
		}

		public override string GetSourceCode(string code)
		{
			return code;
		}

		public override bool HasAutomaticChecking()
		{
			return true;
		}

		public override IEnumerable<SlideBlock> BuildUp(SlideBuildingContext context, IImmutableSet<string> filesInProgress)
		{
			Language ??= LanguageHelpers.GuessByExtension(new FileInfo(UserCodeFilePath));
			Slide = context.Slide;
			CourseId = context.CourseId;
			InterpretNonJsonOutputAs ??= InterpretNonJsonOutputType.SandboxError;
			UnitDirectoryPathRelativeToCourse = context.UnitDirectory.GetRelativePath(context.CourseDirectory);
			ExpectedOutput ??= "";
			var fp = GetFilesProvider(context.CourseDirectory.FullName);
			SolutionRegionContent = new Lazy<string>(() => GetRegionContent(fp.CorrectSolutionFile));
			InitialRegionContent = new Lazy<string>(() => GetRegionContent(fp.InitialUserCodeFile));
			var commentSymbols = Language.GetAttribute<CommentSymbolsAttribute>()?.CommentSymbols ?? "//";
			ExerciseInitialCode = NoStudentZip
				? GetNoStudentZipInitialCode(fp)
				: ExerciseInitialCode ?? $"{commentSymbols} Вставьте сюда финальное содержимое файла {UserCodeFilePath}";
			Validator.ValidatorName = string.Join(" ", Language.GetName(), Validator.ValidatorName ?? "");
			Extractor = Region != null
				? new CommonSingleRegionExtractor((fp.InitialUserCodeFile.Exists ? fp.InitialUserCodeFile : fp.UserCodeFile).ContentAsUtf8())
				: null;
			yield return this;

			var correctSolution = GetCorrectSolution(fp);
			if (correctSolution != null)
			{
				yield return new MarkdownBlock("### Решение") { Hide = true };
				yield return new CodeBlock(correctSolution, Language) { Hide = true };
			}
		}

		public string GetCorrectSolution(FilesProvider fp)
		{
			return SolutionRegionContent.Value ?? (fp.CorrectSolutionFile.Exists ? fp.CorrectSolutionFile.ContentAsUtf8() : null);
		}

		public string GetInitialCode(FilesProvider fp)
		{
			return NoStudentZip ? GetNoStudentZipInitialCode(fp) : fp.InitialUserCodeFile.ContentAsUtf8();
		}

		private string GetNoStudentZipInitialCode(FilesProvider fp)
		{
			return ExerciseInitialCode
				= InitialRegionContent.Value
				?? (fp.InitialUserCodeFile.Exists ? fp.InitialUserCodeFile.ContentAsUtf8() : null)
				?? ExerciseInitialCode
				?? "";
		}

		public string GetRegionContent(FileInfo file)
		{
			return Region == null || !file.Exists
				? null
				: new CommonSingleRegionExtractor(file.ContentAsUtf8()).GetRegion(new Label { Name = Region });
		}

		public override SolutionBuildResult BuildSolution(string userWrittenCode, Language? language = null)
		{
			if (language != null)
				Validator.ValidatorName = string.Join(" ", language.GetName(), Validator.ValidatorName ?? "");

			var validator = ValidatorsRepository.Get(Validator);
			var fullCode = userWrittenCode;
			if (validator != null && Extractor != null)
			{
				fullCode = Extractor
					.ReplaceRegionContent(new Label { Name = Region }, userWrittenCode)
					.RemoveCommonNesting();
				return validator.ValidateSingleFileSolution(userWrittenCode, fullCode);
			}

			return validator != null
				? validator.ValidateSolution(userWrittenCode, fullCode)
				: new SolutionBuildResult(userWrittenCode);
		}

		public override RunnerSubmission CreateSubmission(string submissionId, string code, string courseDirectory)
		{
			using var stream = new ExerciseCheckerZipBuilder(this, GetFilesProvider(courseDirectory)).GetZipForChecker(code);
			return new CommandRunnerSubmission
			{
				Id = submissionId,
				ZipFileData = stream.ToArray(),
				// ReSharper disable once PossibleInvalidOperationException
				Language = Language.Value,
				DockerImageName = DockerImageName,
				RunCommand = RunCommand,
				TimeLimit = TimeLimit,
				InterpretNonJsonOutputAs = InterpretNonJsonOutputAs ?? InterpretNonJsonOutputType.SandboxError
			};
		}

		public FilesProvider GetFilesProvider(string courseDirectory)
		{
			return new FilesProvider(this, courseDirectory);
		}

		public class FilesProvider
		{
			public readonly string CourseDirectory;

			private readonly UniversalExerciseBlock exerciseBlock;

			public FilesProvider(UniversalExerciseBlock exerciseBlock, string courseDirectory)
			{
				CourseDirectory = courseDirectory;
				this.exerciseBlock = exerciseBlock;
			}

			public DirectoryInfo ExerciseDirectory => new(Path.Combine(UnitDirectory.FullName, exerciseBlock.ExerciseDirPath));

			public DirectoryInfo UnitDirectory => new(Path.Combine(CourseDirectory, exerciseBlock.UnitDirectoryPathRelativeToCourse));

			public FileInfo UserCodeFile => ExerciseDirectory.GetFile(exerciseBlock.UserCodeFilePath);

			public FileInfo InitialUserCodeFile => ExerciseDirectory.GetFile(exerciseBlock.InitialUserCodeFilePath);

			public FileInfo CorrectSolutionFile => exerciseBlock.SolutionFilePath == null ? UserCodeFile : ExerciseDirectory.GetFile(exerciseBlock.SolutionFilePath);

			public List<FileInfo> WrongAnswerFiles =>
				wrongAnswerPatterns.SelectMany(p => ExerciseDirectory.GetFiles(p, SearchOption.AllDirectories))
					.ToList();

			public List<FileInfo> InitialFiles =>
				initialPatterns.SelectMany(p => ExerciseDirectory.GetFiles(p, SearchOption.AllDirectories))
					.ToList();
		}

		public class ExerciseStudentZipBuilder
		{
			private readonly UniversalExerciseBlock exerciseBlock;
			private readonly FilesProvider filesProvider;

			public ExerciseStudentZipBuilder(UniversalExerciseBlock exerciseBlock, FilesProvider filesProvider)
			{
				this.exerciseBlock = exerciseBlock;
				this.filesProvider = filesProvider;
			}

			public MemoryStream GetZipMemoryStreamForStudent()
			{
				var excluded = (exerciseBlock.PathsToExcludeForStudent ?? Array.Empty<string>())
					.Concat(initialPatterns)
					.Concat(wrongAnswerPatterns)
					.Concat(solutionPatterns)
					.Concat(new[] { "/checking/", "/bin/", "/obj/", ".idea/", ".vs/" })
					.ToList();

				var directoriesToInclude = exerciseBlock.PathsToIncludeForStudent
					.EmptyIfNull()
					.Select(pathToInclude => new DirectoryInfo(Path.Combine(filesProvider.UnitDirectory.FullName, pathToInclude)))
					.Select(d => d.FullName)
					.Append(filesProvider.ExerciseDirectory.FullName)
					.ToList();

				var toUpdate = ReplaceWithInitialFiles(filesProvider).ToList();

				var zipMemoryStream = ZipUtils.CreateZipFromDirectory(directoriesToInclude, excluded, toUpdate);

				Log.Info($"Собираю zip-архив для студента: zip-архив собран, {zipMemoryStream.Length} байтов");
				return zipMemoryStream;
			}

			private static IEnumerable<FileContent> ReplaceWithInitialFiles(FilesProvider fp)
			{
				foreach (var initialFile in fp.InitialFiles)
				{
					var relativeFilePath = initialFile.GetRelativePath(fp.ExerciseDirectory.FullName);
					var notInitial = InitialFilePathToNotInitial(relativeFilePath);
					yield return new FileContent { Path = notInitial, Data = initialFile.ReadAllContent() };
				}
			}
		}

		private class ExerciseCheckerZipBuilder : IExerciseCheckerZipBuilder
		{
			private readonly UniversalExerciseBlock exerciseBlock;
			private readonly FilesProvider fp;

			public ExerciseCheckerZipBuilder(UniversalExerciseBlock exerciseBlock, FilesProvider fp)
			{
				this.exerciseBlock = exerciseBlock;
				this.fp = fp;
			}

			public Slide Slide => exerciseBlock.Slide;
			public string CourseId => exerciseBlock.CourseId;

			public MemoryStream GetZipForChecker()
			{
				Log.Info($"Собираю zip-архив для проверки: курс {CourseId}, слайд «{Slide?.Title}» ({Slide?.Id})");
				var excluded = (exerciseBlock.PathsToExcludeForChecker ?? Array.Empty<string>())
					.Concat(initialPatterns)
					.Concat(wrongAnswerPatterns)
					.Concat(solutionPatterns)
					.Concat(new[] { "/bin/", "/obj/", ".idea/", ".vs/" })
					.ToList();

				var toUpdateDirectories = exerciseBlock.PathsToIncludeForChecker
					.EmptyIfNull()
					.Select(pathToInclude => new DirectoryInfo(Path.Combine(fp.UnitDirectory.FullName, pathToInclude)))
					.Select(d => d.FullName);
				var directoriesToInclude = toUpdateDirectories.Append(fp.ExerciseDirectory.FullName).ToList();

				using (CourseLock.AcquireReaderLockAsync(CourseId).Result)
				{
					var ms = ZipUtils.CreateZipFromDirectory(directoriesToInclude, excluded, null);

					Log.Info($"Собираю zip-архив для проверки: zip-архив собран, {ms.Length} байтов");
					return ms;
				}
			}

			public MemoryStream GetZipForChecker(string code)
			{
				var codeFile = GetCodeFile(code);
				return ExerciseCheckerZipsCache.GetZip(this, codeFile.Path, codeFile.Data, fp.CourseDirectory);
			}

			private FileContent GetCodeFile(string code)
			{
				if (exerciseBlock.Region == null)
					return new FileContent { Path = exerciseBlock.UserCodeFilePath, Data = Encoding.UTF8.GetBytes(code) };
				var fullCode = new CommonSingleRegionExtractor((fp.InitialUserCodeFile.Exists ? fp.InitialUserCodeFile : fp.UserCodeFile).ContentAsUtf8())
					.ReplaceRegionContent(new Label { Name = exerciseBlock.Region }, code);
				return new FileContent { Path = exerciseBlock.UserCodeFilePath, Data = Encoding.UTF8.GetBytes(fullCode) };
			}
		}
	}
}