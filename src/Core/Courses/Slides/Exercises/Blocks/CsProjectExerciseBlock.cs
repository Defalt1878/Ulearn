using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Microsoft.Build.Evaluation;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides.Blocks;
using Ulearn.Core.Extensions;
using Ulearn.Core.Helpers;
using Ulearn.Core.NUnitTestRunning;
using Ulearn.Core.Properties;
using Ulearn.Core.RunCheckerJobApi;
using Vostok.Logging.Abstractions;

namespace Ulearn.Core.Courses.Slides.Exercises.Blocks
{
	[XmlType("exercise.csproj")]
	public class CsProjectExerciseBlock : AbstractExerciseBlock
	{
		public const string BuildingTargetFrameworkVersion = "4.7.2";
		public const string BuildingTargetNetCoreFrameworkVersion = "3.1";
		public const string BuildingToolsVersion = null;

		public CsProjectExerciseBlock()
		{
			HideExpectedOutputOnError = true;
			HideShowSolutionsButton = true;
			BuildEnvironmentOptions = new BuildEnvironmentOptions
			{
				TargetFrameworkVersion = BuildingTargetFrameworkVersion,
				TargetNetCoreFrameworkVersion = BuildingTargetNetCoreFrameworkVersion,
				ToolsVersion = BuildingToolsVersion
			};
		}

		private static ILog log => LogProvider.Get().ForContext(typeof(CsProjectExerciseBlock));

		[XmlAttribute("csproj")]
		public string CsProjFilePath { get; set; }

		[XmlElement("startupObject")]
		public string StartupObject { get; set; } = "checking.CheckerRunner";

		[XmlElement("userCodeFile")]
		public string UserCodeFilePath { get; set; }

		[XmlElement("solutionFile")]
		public string SolutionFilePath { get; set; } // По умолчанию userCodeFile.solution.cs

		[XmlElement("excludePathForChecker")]
		public string[] PathsToExcludeForChecker { get; set; }

		[XmlElement("nunitTestClass")]
		public string[] NUnitTestClasses { get; set; }

		[XmlElement("excludePathForStudent")]
		public string[] PathsToExcludeForStudent { get; set; }

		[XmlElement("studentZipIsCompilable")]
		public bool StudentZipIsCompilable { get; set; } = true;

		public string ExerciseDirName => Path.GetDirectoryName(CsProjFilePath).EnsureNotNull("csproj должен быть в поддиректории");

		public string CsprojFileName => Path.GetFileName(CsProjFilePath);

		public string UserCodeFileNameWithoutExt => Path.GetFileNameWithoutExtension(UserCodeFilePath);

		public string CorrectFullSolutionFileName => $"{UserCodeFileNameWithoutExt}.Solution.cs";

		private Regex WrongAnswersAndSolutionNameRegex => new(new Regex("^") + UserCodeFileNameWithoutExt + new Regex("\\.(.+)\\.cs"), RegexOptions.IgnoreCase);

		[XmlIgnore]
		public string UnitDirectoryPathRelativeToCourse { get; set; }

		public BuildEnvironmentOptions BuildEnvironmentOptions { get; set; }

		[XmlIgnore]
		public Slide Slide { get; private set; }

		[XmlIgnore]
		public string CourseId { get; private set; }

		public static string SolutionFilepathToUserCodeFilepath(string solutionFilepath)
		{
			// cut .solution.cs
			var userCodeFilenameWithoutExt = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(solutionFilepath));
			var userCodeFilename = $"{userCodeFilenameWithoutExt}.cs";
			var path = Path.GetDirectoryName(solutionFilepath);
			return path == null ? userCodeFilename : Path.Combine(path, userCodeFilename);
		}

		public bool IsWrongAnswer(string name)
		{
			return WrongAnswersAndSolutionNameRegex.IsMatch(name) && !name.Contains(".Solution.");
		}

		public override bool HasAutomaticChecking()
		{
			return Language == Common.Language.CSharp;
		}

		public override IEnumerable<SlideBlock> BuildUp(SlideBuildingContext context, IImmutableSet<string> filesInProgress)
		{
			Language ??= context.CourseSettings.DefaultLanguage;
			Slide = context.Slide;
			CourseId = context.CourseId;
			ExerciseInitialCode ??= "// Вставьте сюда финальное содержимое файла " + UserCodeFilePath;
			ExpectedOutput ??= "";
			Validator.ValidatorName = string.Join(" ", Language.GetName(), Validator.ValidatorName ?? "");
			UnitDirectoryPathRelativeToCourse = context.UnitDirectory.GetRelativePath(context.CourseDirectory);

			ReplaceStartupObjectForNUnitExercises();

			yield return this;

			var filesProvider = new FilesProvider(this, context.CourseDirectory.FullName);
			var correctSolutionFile = filesProvider.CorrectSolutionFile;
			if (correctSolutionFile.Exists)
			{
				yield return new MarkdownBlock("### Решение") { Hide = true };
				yield return new CodeBlock(correctSolutionFile.ContentAsUtf8(), Language) { Hide = true };
			}
		}

		public void ReplaceStartupObjectForNUnitExercises()
		{
			/* Replace StartupObject if exercise uses NUnit tests. It should be after CreateZipForStudent() call */
			var useNUnitLauncher = NUnitTestClasses != null;
			StartupObject = useNUnitLauncher ? typeof(NUnitTestRunner).FullName : StartupObject;
		}

		public override string GetSourceCode(string code)
		{
			return code;
		}

		public override SolutionBuildResult BuildSolution(string userWrittenCodeFile, Language? language = null)
		{
			var validator = ValidatorsRepository.Get(Validator);
			return validator.ValidateSolution(userWrittenCodeFile, userWrittenCodeFile);
		}

		public override RunnerSubmission CreateSubmission(string submissionId, string code, string courseDirectory)
		{
			var exerciseCheckerZipBuilder = new ExerciseCheckerZipBuilder(this, GetFilesProvider(courseDirectory));
			using var stream = exerciseCheckerZipBuilder.GetZipForChecker(code);
			return new ProjRunnerSubmission
			{
				Id = submissionId,
				ZipFileData = stream.ToArray(),
				ProjectFileName = CsprojFileName,
				Input = "",
				NeedRun = true,
				TimeLimit = TimeLimit
			};
		}

		public FilesProvider GetFilesProvider(string courseDirectory)
		{
			return new FilesProvider(this, courseDirectory);
		}

		public class FilesProvider
		{
			public readonly string CourseDirectory;

			private readonly CsProjectExerciseBlock exerciseBlock;

			public FilesProvider(CsProjectExerciseBlock exerciseBlock, string courseDirectory)
			{
				CourseDirectory = courseDirectory;
				this.exerciseBlock = exerciseBlock;
			}

			public FileInfo CsprojFile => ExerciseDirectory.GetFile(exerciseBlock.CsprojFileName);

			public FileInfo UserCodeFile => ExerciseDirectory.GetFile(exerciseBlock.UserCodeFilePath);

			public DirectoryInfo UserCodeFileParentDirectory => UserCodeFile.Directory;

			// В отличие от GetorrectFullSolutionFile, здесь может содержаться не полное, а промежуточное решение серии задач, состоящей из доработток одного файла.
			public FileInfo CorrectSolutionFile => exerciseBlock.SolutionFilePath == null ? CorrectFullSolutionFile : ExerciseDirectory.GetFile(exerciseBlock.SolutionFilePath);

			public FileInfo CorrectFullSolutionFile => UserCodeFileParentDirectory.GetFile(exerciseBlock.CorrectFullSolutionFileName);

			public string CorrectFullSolutionPath => CorrectFullSolutionFile.GetRelativePath(ExerciseDirectory);

			public DirectoryInfo ExerciseDirectory => new(Path.Combine(CourseDirectory, exerciseBlock.UnitDirectoryPathRelativeToCourse, exerciseBlock.ExerciseDirName));
		}

		private class ExerciseCheckerZipBuilder : IExerciseCheckerZipBuilder
		{
			private readonly CsProjectExerciseBlock exerciseBlock;
			private readonly FilesProvider filesProvider;

			public ExerciseCheckerZipBuilder(CsProjectExerciseBlock exerciseBlock, FilesProvider filesProvider)
			{
				this.exerciseBlock = exerciseBlock;
				this.filesProvider = filesProvider;
			}

			public Slide Slide => exerciseBlock.Slide;
			public string CourseId => exerciseBlock.CourseId;

			public MemoryStream GetZipForChecker()
			{
				log.Info($"Собираю zip-архив для проверки: курс {CourseId}, слайд «{Slide?.Title}» ({Slide?.Id})");
				var excluded = (exerciseBlock.PathsToExcludeForChecker ?? Array.Empty<string>())
					.Concat(new[] { "/bin/", "/obj/", ".idea/", ".vs/" })
					.ToList();

				using (CourseLock.AcquireReaderLockAsync(CourseId).Result)
				{
					var toUpdate = GetAdditionalFiles(excluded, filesProvider).ToList();
					log.Info($"Собираю zip-архив для проверки: дополнительные файлы [{string.Join(", ", toUpdate.Select(c => c.Path))}]");

					var ms = ZipUtils.CreateZipFromDirectory(new List<string> { filesProvider.ExerciseDirectory.FullName }, excluded, toUpdate);
					log.Info($"Собираю zip-архив для проверки: zip-архив собран, {ms.Length} байтов");
					return ms;
				}
			}

			public MemoryStream GetZipForChecker(string code)
			{
				var codeFile = GetCodeFile(code);
				return ExerciseCheckerZipsCache.GetZip(this, codeFile.Path, codeFile.Data, filesProvider.CourseDirectory);
			}

			private FileContent GetCodeFile(string code)
			{
				return new FileContent { Path = exerciseBlock.UserCodeFilePath, Data = Encoding.UTF8.GetBytes(code) };
			}

			private IEnumerable<FileContent> GetAdditionalFiles(List<string> excluded, FilesProvider fp)
			{
				var useNUnitLauncher = exerciseBlock.NUnitTestClasses != null;

				using (var stream = ProjModifier.ModifyCsproj(fp.CsprojFile, ModifyCsproj(excluded, useNUnitLauncher, fp), exerciseBlock.BuildEnvironmentOptions.ToolsVersion))
				{
					yield return new FileContent
					{
						Path = exerciseBlock.CsprojFileName,
						Data = stream.ToArray()
					};
				}

				if (useNUnitLauncher)
					yield return new FileContent { Path = GetNUnitTestRunnerFilename(), Data = CreateTestLauncherFile() };

				foreach (var fileContent in ExerciseStudentZipBuilder.ResolveCsprojLinks(exerciseBlock, fp))
					yield return fileContent;
			}

			private static string GetNUnitTestRunnerFilename()
			{
				return nameof(NUnitTestRunner) + ".cs";
			}

			private Action<Project> ModifyCsproj(List<string> excluded, bool addNUnitLauncher, FilesProvider fp)
			{
				return proj =>
				{
					ProjModifier.PrepareForCheckingUserCode(proj, exerciseBlock, excluded, fp);
					if (addNUnitLauncher)
						proj.AddItem("Compile", GetNUnitTestRunnerFilename());

					ProjModifier.SetBuildEnvironmentOptions(proj, exerciseBlock.BuildEnvironmentOptions);
				};
			}

			private byte[] CreateTestLauncherFile()
			{
				var data = Resources.NUnitTestRunner;

				var oldTestFilter = "\"SHOULD_BE_REPLACED\"";
				var newTestFilter = string.Join(",", exerciseBlock.NUnitTestClasses.Select(x => $"\"{x}\""));
				var newData = data.Replace(oldTestFilter, newTestFilter);

				newData = newData.Replace("WillBeMain", "Main");

				return Encoding.UTF8.GetBytes(newData);
			}
		}
	}
}