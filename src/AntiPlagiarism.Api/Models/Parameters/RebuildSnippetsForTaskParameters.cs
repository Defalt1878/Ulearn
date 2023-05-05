using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Ulearn.Common;

namespace AntiPlagiarism.Api.Models.Parameters;

public class RebuildSnippetsForTaskParameters : AntiPlagiarismApiParameters
{
	[BindRequired]
	[FromQuery(Name = "taskId")]
	public Guid TaskId { get; set; }

	[BindRequired]
	[FromQuery(Name = "language")]
	public Language Language { get; set; }
}