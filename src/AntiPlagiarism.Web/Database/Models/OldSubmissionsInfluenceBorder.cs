using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AntiPlagiarism.Web.Database.Models;

public class OldSubmissionsInfluenceBorder
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	public DateTime? Date { get; set; }
}