using System.Collections.Generic;

namespace AntiPlagiarism.ConsoleApp.Models;

public class SubmissionsInfo
{
	public readonly List<Author> Authors = new();
	public readonly List<TaskInfo> Tasks = new();
	public readonly List<SubmissionInfo> Submissions = new();
}