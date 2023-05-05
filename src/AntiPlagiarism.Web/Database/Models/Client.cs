using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AntiPlagiarism.Web.Database.Models;

public class Client
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	[Required]
	public Guid Token { get; set; }

	[Required]
	[MaxLength(200)]
	public string Name { get; set; }

	[Required]
	public bool IsEnabled { get; set; }
}