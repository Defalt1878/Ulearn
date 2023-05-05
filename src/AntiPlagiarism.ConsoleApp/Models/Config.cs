using System.Collections.Generic;
using Ulearn.Common;

namespace AntiPlagiarism.ConsoleApp.Models
{
	public class Config
	{
		public const string EndPointUrl = "http://localhost:33333/";
		public const int MaxCodeLinesCount = 1000;
		public const int MaxInQuerySubmissionsCount = 3;
		public readonly List<Language> ExcludedLanguages = new() { Language.Text };
		public readonly List<string> ExcludedPaths = new() { "node_modules", "antiplagiarism_app" };
		public string Token;
	}
}