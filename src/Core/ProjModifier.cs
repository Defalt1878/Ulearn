using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Evaluation;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Slides.Exercises.Blocks;
using Ulearn.Core.Helpers;

namespace Ulearn.Core
{
	public class FileToCopy
	{
		public FileToCopy(string sourceFile, string destinationFile)
		{
			SourceFile = sourceFile;
			DestinationFile = destinationFile;
		}

		public readonly string SourceFile;
		public readonly string DestinationFile;
	}

	public static class ProjModifier
	{
		public static MemoryStream ModifyCsproj(FileInfo csproj, Action<Project> changingAction, string toolsVersion = null)
		{
			MsBuildLocationHelper.InitPathToMsBuild();
			return FuncUtils.Using(
				new ProjectCollection(ToolsetDefinitionLocations.Registry | ToolsetDefinitionLocations.ConfigurationFile),
				projectCollection =>
				{
					var proj = new Project(csproj.FullName, null, toolsVersion, projectCollection);
					return ModifyCsproj(proj, changingAction);
				},
				projectCollection => projectCollection.UnloadAllProjects());
		}

		private static MemoryStream ModifyCsproj(Project proj, Action<Project> changingAction)
		{
			changingAction?.Invoke(proj);
			var memoryStream = StaticRecyclableMemoryStreamManager.Manager.GetStream();
			using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8, 1024, true)) // Без leaveOpen закрытие StreamWriter закроет memoryStream
			{
				proj.Save(streamWriter);
				memoryStream.Position = 0;
				return memoryStream;
			}
		}

		public static void PrepareForStudentZip(Project proj, CsProjectExerciseBlock ex, CsProjectExerciseBlock.FilesProvider fp)
		{
			var toExclude = FindItemNames(proj, file => ExerciseStudentZipBuilder.NeedExcludeFromStudentZip(ex, file)).ToList();
			var solutionsOfOtherTasks = toExclude.Where(n => ExerciseStudentZipBuilder.IsAnySolution(n) && fp.CorrectFullSolutionPath != n).ToList();

			/* Remove StartupObject from csproj: it's not needed in student zip */
			var startupObject = proj.GetProperty("StartupObject");
			if (startupObject != null)
				proj.RemoveProperty(startupObject);

			RemoveCheckingFromCsproj(proj);

			var userCodeFilepathsOfOtherTasks = solutionsOfOtherTasks.Select(CsProjectExerciseBlock.SolutionFilepathToUserCodeFilepath);
			SetFilepathItemTypeToCompile(proj, userCodeFilepathsOfOtherTasks.Concat(new[] { ex.UserCodeFilePath }));

			ReplaceLinksWithItems(proj);

			ExcludePaths(proj, toExclude);
		}

		public static void RemoveCheckingFromCsproj(Project proj)
		{
			var toRemove = proj.Items.Where(IsChecking).ToList();
			proj.RemoveItems(toRemove);
		}

		private static bool IsChecking(ProjectItem item)
		{
			return (item.EvaluatedInclude.StartsWith("checking" + Path.DirectorySeparatorChar) &&
					item.UnevaluatedInclude != "**/*$(DefaultLanguageSourceExtension)") ||
					item.DirectMetadata.Any(md => md.Name == "Link" &&
												md.UnevaluatedValue != "**/*$(DefaultLanguageSourceExtension)" &&
												md.EvaluatedValue.StartsWith("checking" + Path.DirectorySeparatorChar)
					);
		}

		public static void PrepareForCheckingUserCode(Project proj, CsProjectExerciseBlock ex, List<string> excludedPaths, CsProjectExerciseBlock.FilesProvider fp)
		{
			var solutionRelativePath = fp.ExerciseDirectory.GetRelativePathsOfFiles()
				.SingleOrDefault(n => n.Equals(fp.CorrectFullSolutionPath, StringComparison.InvariantCultureIgnoreCase));

			if (solutionRelativePath != null)
				excludedPaths.Add(solutionRelativePath);

			SetFilepathItemTypeToCompile(proj, ex.UserCodeFilePath);
			PrepareForChecking(proj, ex.StartupObject, excludedPaths);
		}

		public static void SetFilepathItemTypeToCompile(Project proj, IEnumerable<string> files)
		{
			foreach (var f in files)
				SetFilepathItemType(proj, f, "Compile");
		}

		public static void SetFilepathItemTypeToCompile(Project proj, string fileName) => SetFilepathItemType(proj, fileName, "Compile");

		public static void SetFilepathItemType(Project proj, string fileName, string type)
		{
			var projectItem = proj.Items.SingleOrDefault(i => i.UnevaluatedInclude.Equals(fileName, StringComparison.InvariantCultureIgnoreCase));
			if (projectItem != null) // может быть null, если это промежуточное решение задачи (ULEARN-485)
				projectItem.ItemType = type;
		}

		private static IEnumerable<string> FindItemNames(Project proj, Func<string, bool> predicate)
		{
			return proj.Items.Select(i => i.UnevaluatedInclude).Where(predicate);
		}

		public static void PrepareForChecking(Project proj, string startupObject, IReadOnlyList<string> excludedPaths)
		{
			proj.SetProperty("StartupObject", startupObject);
			proj.SetProperty("OutputType", "Exe");
			proj.SetProperty("UseVSHostingProcess", "false");
			ReplaceLinksWithItems(proj);
			ExcludePaths(proj, excludedPaths);
		}

		private static void ExcludePaths(Project proj, IReadOnlyList<string> excludedPaths)
		{
			var toRemove = proj.Items.Where(item => excludedPaths.Contains(item.UnevaluatedInclude, StringComparer.InvariantCultureIgnoreCase)).ToList();
			proj.RemoveItems(toRemove);
		}

		public static void SetBuildEnvironmentOptions(Project proj, BuildEnvironmentOptions options)
		{
			var frameworkName = proj.GetPropertyValue("TargetFramework");
			if (frameworkName.Contains("netcore"))
				proj.SetProperty("TargetFrameworkVersion", options.TargetNetCoreFrameworkVersion);
			else
				proj.SetProperty("TargetFrameworkVersion", options.TargetFrameworkVersion);
			proj.SetProperty("Features", "peverify-compat"); // https://developercommunity.visualstudio.com/content/problem/503750/operation-could-destabilize-the-runtime-and-peveri.html
		}

		public static List<FileToCopy> ReplaceLinksWithItemsAndReturnWhatToCopy(Project project)
		{
			var linkedItems = (from item in project.Items
				let meta = item.DirectMetadata.FirstOrDefault(md => md.Name == "Link")
				where meta != null
				select new { item, newPath = ChangeNameToGitIgnored(meta.EvaluatedValue) }).ToList();
			var copies = new List<FileToCopy>();
			foreach (var link in linkedItems)
			{
				copies.Add(new FileToCopy(link.item.EvaluatedInclude, link.newPath));
				link.item.UnevaluatedInclude = link.newPath;
				link.item.RemoveMetadata("Link");
			}

			return copies;
		}

		public static void ReplaceLinksWithItems(Project project)
		{
			ReplaceLinksWithItemsAndReturnWhatToCopy(project);
		}

		private static string ChangeNameToGitIgnored(string filename)
		{
			var d = Path.GetDirectoryName(filename) ?? "";
			var fn = Path.GetFileName(filename);
			return Path.Combine(d, "~$" + fn);
		}
	}

	public class BuildEnvironmentOptions
	{
		public string TargetFrameworkVersion { get; set; }
		public string TargetNetCoreFrameworkVersion { get; set; }
		public string ToolsVersion { get; set; }
	}
}