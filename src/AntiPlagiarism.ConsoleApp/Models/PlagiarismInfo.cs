using CsvHelper.Configuration.Attributes;
using Ulearn.Common;

namespace AntiPlagiarism.ConsoleApp.Models;

public class PlagiarismInfo
{
	[Name("Автор решения")]
	public string AuthorName { get; set; }

	[Name("Название задачи")]
	public string TaskTitle { get; set; }

	[Name("Автор с наиболее похожим решением")]
	public string PlagiarismAuthorName { get; set; }

	[Name("Уровень схожести")]
	public string SuspicionLevel { get; set; }

	[Name("Процент схожести решений")]
	public string Weight { get; set; }

	[Name("Язык решения")]
	public Language Language { get; set; }

	[Name("Количество списываний студента")]
	public int PlagiarismCount { get; set; }

	[Ignore]
	public string Code { get; set; }

	[Ignore]
	public string PlagiarismCode { get; set; }
}