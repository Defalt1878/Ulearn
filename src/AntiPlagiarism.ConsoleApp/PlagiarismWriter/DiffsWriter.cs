using System.Collections.Generic;
using System.IO;
using AntiPlagiarism.ConsoleApp.Models;
using Ulearn.Common.Extensions;

namespace AntiPlagiarism.ConsoleApp.PlagiarismWriter;

public class DiffsWriter
{
	private readonly string plagiarismsDirectory;
	private readonly PlagiarismsInfoCsvWriter csvWriter;

	public DiffsWriter(string path)
	{
		plagiarismsDirectory = path.PathCombine(Repository.AntiplagiarismDataDirectory).PathCombine("diffs");
		csvWriter = new PlagiarismsInfoCsvWriter(path);
	}

	public void WritePlagiarisms(List<PlagiarismInfo> plagiarisms)
	{
		csvWriter.WritePlagiarism(plagiarisms);

		if (!Directory.Exists(plagiarismsDirectory))
			Directory.CreateDirectory(plagiarismsDirectory);

		foreach (var plagiarism in plagiarisms)
		{
			var currentPlagiarismPath = plagiarismsDirectory.PathCombine(plagiarism.AuthorName);
			if (!Directory.Exists(currentPlagiarismPath))
				Directory.CreateDirectory(currentPlagiarismPath!);

			currentPlagiarismPath = currentPlagiarismPath.PathCombine(plagiarism.TaskTitle);
			if (!Directory.Exists(currentPlagiarismPath))
				Directory.CreateDirectory(currentPlagiarismPath!);

			WriteCodeInFile(currentPlagiarismPath, plagiarism.AuthorName, plagiarism.TaskTitle, plagiarism.Code);
			WriteCodeInFile(currentPlagiarismPath, plagiarism.PlagiarismAuthorName, plagiarism.TaskTitle, plagiarism.PlagiarismCode);
		}
	}

	private static void WriteCodeInFile(string path, string authorName, string taskTitle, string content) => 
		File.WriteAllText(path.PathCombine($"{taskTitle} {authorName}.txt"), content);
}