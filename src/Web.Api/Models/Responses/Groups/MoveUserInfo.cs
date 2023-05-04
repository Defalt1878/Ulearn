using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Groups;

[DataContract]
public class MoveUserInfo
{
	[DataMember]
	public string UserName { get; set; }

	[DataMember]
	public string UserId { get; set; }

	[DataMember]
	public int OldGroupId { get; set; }

	[DataMember]
	public int CurrentGroupId { get; set; }
}