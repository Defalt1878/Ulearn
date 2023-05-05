using System;
using AntiPlagiarism.Api.Defaults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Ulearn.Common;

namespace AntiPlagiarism.Api.Models.Parameters;

public class GetAuthorPlagiarismsParameters : AntiPlagiarismApiParameters
{
	[BindRequired]
	[FromQuery(Name = "authorId")]
	public Guid AuthorId { get; set; }

	[BindRequired]
	[FromQuery(Name = "taskId")]
	public Guid TaskId { get; set; }

	[BindRequired]
	[FromQuery(Name = "language")]
	public Language Language { get; set; }

	[FromQuery(Name = "lastSubmissionsCount")]
	public int LastSubmissionsCount { get; set; } = GetAuthorPlagiarismsDefaults.LastSubmissionsCount;
}