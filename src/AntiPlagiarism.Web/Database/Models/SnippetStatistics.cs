using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ulearn.Common;

namespace AntiPlagiarism.Web.Database.Models;

public class SnippetStatistics
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	public int SnippetId { get; set; }
	public virtual Snippet Snippet { get; set; }

	public Guid TaskId { get; set; }

	public Language Language { get; set; }

	public int ClientId { get; set; }
	public virtual Client Client { get; set; }

	public int AuthorsCount { get; set; }
}