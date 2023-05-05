using System;
using Ulearn.Common;

namespace AntiPlagiarism.Web.Database.Models;

public class TaskStatisticsParameters
{
	public Guid TaskId { get; set; }
	public Language Language { get; set; }

	public double Mean { get; set; }

	public double Deviation { get; set; }

	public int SubmissionsCount { get; set; }

	public DateTime? Timestamp { get; set; }
}