using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ulearn.Common;

namespace AntiPlagiarism.Web.Database.Models;

public class Submission
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	public int ClientId { get; set; }
	public virtual Client Client { get; set; }

	[Required]
	public Guid TaskId { get; set; }

	[Required]
	public Guid AuthorId { get; set; }

	public int ProgramId { get; set; }
	public virtual Code Program { get; set; }

	[StringLength(500)]
	public string AdditionalInfo { get; set; }

	[Required]
	public DateTime AddingTime { get; set; }

	[Required]
	public Language Language { get; set; }

	[Required]
	public int TokensCount { get; set; }

	[NotMapped]
	public string ProgramText => Program.Text;

	[MaxLength(50)]
	public string ClientSubmissionId { get; set; }
}