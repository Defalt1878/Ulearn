using System;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Common;

namespace AntiPlagiarism.Api.Models.Parameters
{
	public class RecalculateTaskStatisticsParameters : AntiPlagiarismApiParameters
	{
		[FromQuery(Name = "fromTask")]
		public Guid? FromTaskId { get; set; }

		[FromQuery(Name = "taskId")]
		public Guid? TaskId { get; set; }

		[FromQuery(Name = "language")]
		public Language Language { get; set; }
	}
}