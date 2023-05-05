using System;
using System.Runtime.Serialization;
using AntiPlagiarism.Api.ModelBinders;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Common;
using Ulearn.Common.Api.Models.Parameters;

namespace AntiPlagiarism.Api.Models.Parameters;

[DataContract]
[ModelBinder(typeof(JsonModelBinder), Name = "parameters")]
public class SetSuspicionLevelsParameters : ApiParameters
{
	[DataMember(Name = "taskId", IsRequired = true)]
	public Guid TaskId { get; set; }

	[DataMember(Name = "language", IsRequired = true)]
	public Language Language { get; set; }

	[DataMember(Name = "faintSuspicion")]
	public double? FaintSuspicion { get; set; }

	[DataMember(Name = "strongSuspicion")]
	public double? StrongSuspicion { get; set; }
}