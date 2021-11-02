﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AntiPlagiarism.Api.Models.Parameters
{
	public class GetProcessingStatusParameters : AntiPlagiarismApiParameters
	{
		[BindRequired]
		[FromQuery(Name = "submissionId")]
		public int SubmissionId { get; set; }
	}
}