﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Models
{
	public class GoogleSheetTask
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		
		[Required]
		[StringLength(100)]
		public string CourseId { get; set; }

		[Required]
		[StringLength(64)]
		public virtual IList<GoogleSheetTaskGroup> GoogleSheetTaskGroups { get; set; }

		[Required]
		[StringLength(64)]
		public string AuthorId { get; set; }

		public virtual ApplicationUser Author { get; set; }

		[Required]
		public bool IsVisibleForStudents { get; set; }

		public DateTime? RefreshStartDate { get; set; }
		
		public DateTime? RefreshEndDate { get; set; }
		
		public int? RefreshTimeInMinutes { get; set; }
		
		[Required]
		public string SpreadsheetId { get; set; }
		
		[Required]
		public int ListId { get; set; }
	}
}