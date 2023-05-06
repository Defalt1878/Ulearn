using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Slides.Blocks;
using Ulearn.Core.Courses.Slides.Exercises.Blocks;

namespace Ulearn.Core.Courses.Slides.Exercises
{
	[XmlRoot("slide.polygon", IsNullable = false, Namespace = "https://ulearn.me/schema/v2")]
	public class PolygonExerciseSlide : ExerciseSlide
	{
		[XmlElement("polygonPath")]
		public string PolygonPath { get; set; }

		public override void Validate(SlideLoadingContext context)
		{
			if (string.IsNullOrEmpty(PolygonPath))
				throw new CourseLoadingException("В slide.polygon должен находиться атрибут polygonPath");
			base.Validate(context);
		}

		public override void BuildUp(SlideLoadingContext context)
		{
			Blocks = GetBlocksProblem(context.CourseId, Id, context.CourseDirectory, context.UnitDirectory.GetRelativePath(context.CourseDirectory))
				.Concat(Blocks.Where(block => block is not MarkdownBlock))
				.ToArray();

			var polygonExercise = Blocks.Single(block => block is PolygonExerciseBlock) as PolygonExerciseBlock;
			polygonExercise!.ExerciseDirPath = Path.Combine(PolygonPath);
			var problem = GetProblem(Path.Combine(context.UnitDirectory.FullName, PolygonPath, "problem.xml"), context.CourseSettings.DefaultLanguage);
			polygonExercise.TimeLimitPerTest = problem.TimeLimit;
			polygonExercise.TimeLimit = (int)Math.Ceiling(problem.TimeLimit * problem.TestCount);
			polygonExercise.UserCodeFilePath = problem.PathAuthorSolution;
			polygonExercise.Language = LanguageHelpers.GuessByExtension(new FileInfo(polygonExercise.UserCodeFilePath));
			polygonExercise.DefaultLanguage = context.CourseSettings.DefaultLanguage;
			polygonExercise.RunCommand = $"python3.8 main.py {polygonExercise.Language} {polygonExercise.TimeLimitPerTest} {polygonExercise.UserCodeFilePath.Split('/', '\\')[1]}";
			Title = problem.Title;
			PrepareSolution(Path.Combine(context.UnitDirectory.FullName, PolygonPath, polygonExercise.UserCodeFilePath));

			base.BuildUp(context);
		}

		private IEnumerable<SlideBlock> GetBlocksProblem(string courseId, Guid slideId, DirectoryInfo courseDirectory, string unitPathRelativeToCourse)
		{
			var statementsPath = Path.Combine(courseDirectory.FullName, unitPathRelativeToCourse, PolygonPath, "statements");
			var markdownBlock = Blocks.FirstOrDefault(block => block is MarkdownBlock);
			if (markdownBlock != null)
			{
				yield return markdownBlock;
			}
			else if (Directory.Exists(Path.Combine(statementsPath, ".html")))
			{
				var htmlDirectoryPath = Path.Combine(statementsPath, ".html", "russian");
				var htmlData = File.ReadAllText(Path.Combine(htmlDirectoryPath, "problem.html"));
				yield return RenderFromHtml(htmlData, courseId, courseDirectory, unitPathRelativeToCourse);
			}

			var pdfLink = PolygonPath + "/statements/.pdf/russian/problem.pdf";
			yield return new MarkdownBlock($"[Скачать условия задачи в формате PDF]({pdfLink})");
		}

		private static readonly Regex problemBodyRegex = new("(<DIV class=[\"']problem-statement['\"]>.*)</BODY>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Regex imageRegex = new("<IMG[^>]+src=\"(?<src>[^\"]+)\"", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private SlideBlock RenderFromHtml(string html, string courseId, DirectoryInfo courseDirectory, string unitPathRelativeToCourse)
		{
			var match = problemBodyRegex.Match(html);
			if (!match.Success)
				throw new Exception();
			var body = match.Groups[1].Value;
			var processedBody = body
				.Replace("$$$$$$", "$$")
				.Replace("$$$", "$");
			processedBody = FixImageLinks(processedBody, courseId, courseDirectory, unitPathRelativeToCourse);

			return new HtmlBlock($"<div class=\"math-tex problem\">{processedBody}</div>");
		}

		private string FixImageLinks(string html, string courseId, DirectoryInfo courseDirectory, string unitPathRelativeToCourse)
		{
			var imagesDirectoryRelativeToUnit = Path.Combine(PolygonPath, "statements", ".html", "russian");
			var imagesDirectory = Path.Combine(courseDirectory.FullName, unitPathRelativeToCourse, imagesDirectoryRelativeToUnit);
			var matches = imageRegex.Matches(html).ToList();
			var originalToReplacement = new Dictionary<string, string>();
			foreach (var match in matches)
			{
				var fileName = match.Groups["src"].Value;
				if (fileName.Contains('/') || fileName.Contains('\\'))
					continue;
				var imageFile = new FileInfo(Path.Combine(imagesDirectory, fileName));
				if (imageFile.Exists)
				{
					var link = CourseUrlHelper.GetAbsoluteUrlToFile(HtmlBlock.BaseUrlApiPlaceholder, courseId, unitPathRelativeToCourse, Path.Combine(imagesDirectoryRelativeToUnit, fileName));
					originalToReplacement.Add(fileName, link);
				}
			}

			return originalToReplacement
				.Aggregate(html, (current, kvp) => current.Replace(kvp.Key, kvp.Value));
		}

		private static MarkdownBlock GetLinkOnPdf(string courseId, string slideId)
		{
			var link = $"/Exercise/GetPdf?courseId={courseId}&slideId={slideId}";
			return new MarkdownBlock($"[Скачать условия задачи в формате PDF]({link})");
		}

		private static Problem GetProblem(string pathToXml, Language? defaultLanguage)
		{
			var document = new XmlDocument();
			document.Load(pathToXml);

			var nameNodeList = document.SelectNodes(@"/problem/names/name");
			var name = GetNodes(nameNodeList)
				.First(node => node.Attributes?["language"]?.Value is "russian")
				.Attributes?["value"]?.Value ?? "";

			var timeLimit = document.SelectSingleNode(@"/problem/judging/testset/time-limit")!.InnerText;
			var testCount = document.SelectSingleNode(@"/problem/judging/testset/test-count")!.InnerText;
			var solutionPath = GetSolutionPath(document, defaultLanguage);
			return new Problem
			{
				TimeLimit = int.Parse(timeLimit) / 1000d,
				TestCount = int.Parse(testCount),
				Title = name,
				PathAuthorSolution = solutionPath
			};
		}

		private static string GetSolutionPath(XmlNode xmlProblem, Language? defaultLanguage)
		{
			var solutionNodeList = xmlProblem.SelectNodes(@"/problem/assets/solutions/solution");
			var solutions = GetNodes(solutionNodeList)
				.Select(node => new
				{
					Tag = node.Attributes!["tag"]?.Value,
					Path = node.ChildNodes.Item(0)!.Attributes!["path"]?.Value
				})
				.ToArray();
			var mainSolution = solutions.First(s => s.Tag == "main");
			var acceptedSolution = solutions.Where(s => s.Tag == "accepted").ToArray();

			var solutionWithLanguageAsDefaultInCourse = new[] { mainSolution }
				.Concat(acceptedSolution)
				.FirstOrDefault(s => LanguageHelpers.GuessByExtension(new FileInfo(s.Path)) == defaultLanguage);

			return solutionWithLanguageAsDefaultInCourse?.Path ?? mainSolution.Path;
		}

		private static IEnumerable<XmlNode> GetNodes(XmlNodeList nodeList)
		{
			return Enumerable.Range(0, nodeList!.Count).Select(nodeList.Item);
		}

		private static void PrepareSolution(string solutionFilename)
		{
			var solution = File.ReadAllText(solutionFilename);
			if (solution.Contains("//region Task") && solution.Contains("//endregion Task"))
				return;
			var solutionWithRegion = $"//region Task\n\n{solution}\n\n//endregion Task";
			File.WriteAllText(solutionFilename, solutionWithRegion);
		}
	}

	internal class Problem
	{
		public double TimeLimit { get; set; }
		public int TestCount { get; set; }
		public string Title { get; set; }
		public string PathAuthorSolution { get; set; }
	}
}