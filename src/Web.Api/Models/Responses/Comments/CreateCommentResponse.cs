using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;

namespace Ulearn.Web.Api.Models.Responses.Comments;

[DataContract]
public class CreateCommentResponse : SuccessResponse
{
	[DataMember]
	public int Id { get; set; }

	[DataMember]
	public string ApiUrl { get; set; }
}