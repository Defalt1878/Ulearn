﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Models
{
	public class GoogleSheetExportTaskGroup
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		
		[Required]
		public int TaskId { get; set; }
		
		[Required]
		public virtual GoogleSheetExportTask Task { get; set; }
		
		[Required]
		public int GroupId { get; set; }

		[Required]
		public virtual Group Group { get; set; }
	}
}