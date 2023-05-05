using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AntiPlagiarism.Web.Database.Models;

public class MostSimilarSubmission
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	[ForeignKey(nameof(Submission))] // Если явно не указать, то создаст копию поля SubmissionId
	public int SubmissionId { get; set; }

	public virtual Submission Submission { get; set; }

	[Required]
	public int SimilarSubmissionId { get; set; }

	public virtual Submission SimilarSubmission { get; set; }

	[Required]
	public double Weight { get; set; }

	[Required]
	public DateTime Timestamp { get; set; }
}