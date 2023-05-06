using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Courses.Slides.Exercises.Blocks;

namespace Ulearn.Core.Helpers
{
	public class ExerciseStudentZipBuilder
	{
		private static readonly Regex anySolutionNameRegex = new("(.+)\\.Solution\\.cs", RegexOptions.IgnoreCase);
		private static readonly Regex anyWrongAnswerNameRegex = new("(.+)\\.(WrongAnswer|WA)\\.(.+)\\.cs", RegexOptions.IgnoreCase);

		public static bool IsAnyWrongAnswerOrAnySolution(string name) => anyWrongAnswerNameRegex.IsMatch(name) || anySolutionNameRegex.IsMatch(name);

		public static bool IsAnySolution(string name) => anySolutionNameRegex.IsMatch(name);

		public void BuildStudentZip(Slide slide, FileInfo zipFile, string courseDirectory)
		{
			switch ((slide as ExerciseSlide)?.Exercise)
			{
				case CsProjectExerciseBlock csBlock:
				{
					var fp = csBlock.GetFilesProvider(courseDirectory);
					var zip = new LazilyUpdatingZip(
						fp.ExerciseDirectory,
						new[] { "checking", "bin", "obj", ".idea", ".vs" },
						file => NeedExcludeFromStudentZip(csBlock, file, fp),
						file => GetFileContentInStudentZip(csBlock, file, fp),
						ResolveCsprojLinks(csBlock, fp),
						zipFile);
					zip.UpdateZip();
					return;
				}
				case UniversalExerciseBlock block:
				{
					var zipBuilder = new UniversalExerciseBlock.ExerciseStudentZipBuilder(block, block.GetFilesProvider(courseDirectory));
					using var studentZipMemoryStream = zipBuilder.GetZipMemoryStreamForStudent();
					using var fs = zipFile.OpenWrite();
					studentZipMemoryStream.CopyTo(fs);

					return;
				}
				default:
					throw new InvalidOperationException($"Can't generate student zip for non-project exercise block: slide \"{slide.Title}\" ({slide.Id})");
			}
		}

		private static bool NeedExcludeFromStudentZip(CsProjectExerciseBlock block, FileInfo file, CsProjectExerciseBlock.FilesProvider fp)
		{
			var relativeFilePath = file.GetRelativePath(fp.ExerciseDirectory.FullName);
			return NeedExcludeFromStudentZip(block, relativeFilePath);
		}

		public static bool NeedExcludeFromStudentZip(CsProjectExerciseBlock block, string filepath)
		{
			return IsAnyWrongAnswerOrAnySolution(filepath) ||
					(block.PathsToExcludeForStudent != null && block.PathsToExcludeForStudent.Any(p => p == filepath));
		}

		private static MemoryStream GetFileContentInStudentZip(CsProjectExerciseBlock block, FileInfo file, CsProjectExerciseBlock.FilesProvider fp)
		{
			if (!file.Name.Equals(block.CsprojFileName, StringComparison.InvariantCultureIgnoreCase))
				return null;
			return ProjModifier.ModifyCsproj(file, proj => ProjModifier.PrepareForStudentZip(proj, block, fp));
		}

		public static IEnumerable<FileContent> ResolveCsprojLinks(CsProjectExerciseBlock block, CsProjectExerciseBlock.FilesProvider fp)
		{
			return ResolveCsprojLinks(fp.CsprojFile, block.BuildEnvironmentOptions.ToolsVersion);
		}

		public static IEnumerable<FileContent> ResolveCsprojLinks(FileInfo csprojFile, string toolsVersion)
		{
			MsBuildLocationHelper.InitPathToMsBuild();
			return FuncUtils.Using(
				new ProjectCollection(ToolsetDefinitionLocations.Registry | ToolsetDefinitionLocations.ConfigurationFile),
				projectCollection =>
				{
					return Body();

					IEnumerable<FileContent> Body()
					{
						var project = new Project(csprojFile.FullName, null, toolsVersion, projectCollection);
						var filesToCopy = ProjModifier.ReplaceLinksWithItemsAndReturnWhatToCopy(project);
						foreach (var fileToCopy in filesToCopy)
						{
							var fullSourcePath = Path.Combine(project.DirectoryPath, fileToCopy.SourceFile);
							yield return new FileContent { Path = fileToCopy.DestinationFile, Data = File.ReadAllBytes(fullSourcePath) };
						}
					}
				},
				projectCollection => projectCollection.UnloadAllProjects());
		}
	}
}