using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;

namespace Ulearn.Web.Api.Models.Responses.Groups;

[DataContract]
public class CopyGroupResponse : SuccessResponse
{
	[DataMember]
	public int Id { get; set; }

	[DataMember]
	public string ApiUrl { get; set; }
}