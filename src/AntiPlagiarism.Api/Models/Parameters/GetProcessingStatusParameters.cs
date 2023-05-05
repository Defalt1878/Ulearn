using System.Runtime.Serialization;
using AntiPlagiarism.Api.ModelBinders;
using Microsoft.AspNetCore.Mvc;

namespace AntiPlagiarism.Api.Models.Parameters;

[DataContract]
[ModelBinder(typeof(JsonModelBinder), Name = "parameters")]
public class GetProcessingStatusParameters : AntiPlagiarismApiParameters
{
	[DataMember(Name = "submissionIds", IsRequired = true)]
	public int[] SubmissionIds { get; set; }
}