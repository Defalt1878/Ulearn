using System.Collections.Generic;
using System.Xml.Serialization;
using Ulearn.Common;
using Ulearn.Core.RunCheckerJobApi;

namespace Ulearn.Core.Courses.Slides.Exercises.Blocks
{
	[XmlType("exercise.polygon")]
	public class PolygonExerciseBlock : UniversalExerciseBlock
	{
		public static readonly Dictionary<Language, LanguageLaunchInfo> LanguagesInfo = new()
		{
			[Common.Language.Java] = new LanguageLaunchInfo
			{
				Compiler = "Java 14",
				CompileCommand = "javac {source}",
				RunCommand = "java {compilation-result-file}"
			},
			[Common.Language.Python3] = new LanguageLaunchInfo
			{
				Compiler = "Python 3.8",
				RunCommand = "python3.8 {source}"
			},
			[Common.Language.JavaScript] = new LanguageLaunchInfo
			{
				Compiler = "Node.js v14.15",
				RunCommand = "node {source}"
			},
			[Common.Language.Cpp] = new LanguageLaunchInfo
			{
				Compiler = "G++ 10.2",
				CompileCommand = "g++ -o {name-executable-file} -O2 {source}",
				RunCommand = "./{name-executable-file}"
			},
			[Common.Language.CSharp] = new LanguageLaunchInfo
			{
				Compiler = "C# .Net 5.0",
				CompileCommand = "dotnet build",
				RunCommand = "./bin/Debug/net5.0/app"
			},
			[Common.Language.C] = new LanguageLaunchInfo
			{
				Compiler = "CC 10.2",
				CompileCommand = "cc -o {name-executable-file} -O {source}",
				RunCommand = "./{name-executable-file}"
			}
		};

		public override string DockerImageName => "algorithms-sandbox";
		public override bool NoStudentZip => true;
		public override bool CheckInitialSolution => false;
		public double TimeLimitPerTest { get; set; }
		public override string[] PathsToExcludeForChecker => new[] { "statements", "statements-sections" };
		public Language? DefaultLanguage { get; set; }

		[XmlElement("showTestDescription")]
		public bool ShowTestDescription { get; set; }

		[XmlElement("pythonVisualizerEnabled")]
		public bool PythonVisualizerEnabled { get; set; }

		public RunnerSubmission CreateSubmission(string submissionId, string code, Language language, string courseDirectory)
		{
			var submission = base.CreateSubmission(submissionId, code, courseDirectory);
			if (submission is not CommandRunnerSubmission commandRunnerSubmission)
				return submission;

			commandRunnerSubmission.RunCommand = RunCommandWithArguments(language);

			return commandRunnerSubmission;
		}

		private string RunCommandWithArguments(Language language)
		{
			return $"python3.8 main.py {language} {TimeLimitPerTest} {UserCodeFilePath.Split('/', '\\')[1]}";
		}
	}
}