﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Models
{
	public class EnabledAdditionalScoringGroup
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		public int GroupId { get; set; }

		public virtual SingleGroup Group { get; set; }

		[Required]
		public string ScoringGroupId { get; set; }
	}
}