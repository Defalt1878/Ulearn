using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AntiPlagiarism.Web.Database.Models;

public class TaskStatisticsSourceData
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	[Required]
	public int Submission1Id { get; set; }

	public virtual Submission Submission1 { get; set; }

	[Required]
	public int Submission2Id { get; set; }

	public virtual Submission Submission2 { get; set; }

	[Required]
	public double Weight { get; set; }
}