using System.ComponentModel.DataAnnotations;

namespace AntiPlagiarism.Web.Database.Models;

public class Code
{
	[Key]
	public int Id { get; set; }

	public string Text { get; set; }
}