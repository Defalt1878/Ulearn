using System;

namespace AntiPlagiarism.ConsoleApp.Models;

public class TaskInfo
{
	public readonly string Title;
	public Guid Id;

	public TaskInfo(string title)
	{
		Title = title;
		Id = Guid.NewGuid();
	}
}