using System;
using System.ComponentModel.DataAnnotations.Schema;
using Ulearn.Common;

namespace AntiPlagiarism.Web.Database.Models;

public class ManualSuspicionLevels
{
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public Guid TaskId { get; set; }

	public Language Language { get; set; }
	public double? FaintSuspicion { get; set; }

	public double? StrongSuspicion { get; set; }

	public DateTime Timestamp { get; set; }
}