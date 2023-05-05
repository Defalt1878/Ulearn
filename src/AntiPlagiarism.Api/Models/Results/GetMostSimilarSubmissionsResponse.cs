using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;

namespace AntiPlagiarism.Api.Models.Results;

[DataContract]
public class GetMostSimilarSubmissionsResponse : SuccessResponse
{
	[DataMember(Name = "mostSimilarSubmissions")]
	public List<MostSimilarSubmissions> MostSimilarSubmissions { get; set; }
}

[DataContract]
public class MostSimilarSubmissions
{
	[DataMember(Name = "submissionIds")]
	public string SubmissionId { get; set; }

	[DataMember(Name = "similarSubmissionId")]
	public string SimilarSubmissionId { get; set; }

	[DataMember(Name = "weight")]
	public double Weight { get; set; }
}