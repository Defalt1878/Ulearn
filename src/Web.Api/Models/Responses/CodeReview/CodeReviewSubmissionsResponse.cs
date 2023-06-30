using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.CodeReview;

[DataContract]
public class CodeReviewSubmissionsResponse
{
	[DataMember]
	public List<ReviewSubmissionInfo> Submissions { get; set; } = null!;
}