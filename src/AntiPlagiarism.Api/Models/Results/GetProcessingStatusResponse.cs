using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;

namespace AntiPlagiarism.Api.Models.Results;

public class GetProcessingStatusResponse : SuccessResponse
{
	[DataMember(Name = "inQueueSubmissionIds")]
	public int[] InQueueSubmissionIds { get; set; }
}