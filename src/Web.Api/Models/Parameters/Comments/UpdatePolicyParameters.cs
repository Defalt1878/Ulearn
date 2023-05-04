using System.Runtime.Serialization;
using Database.Models.Comments;

namespace Ulearn.Web.Api.Models.Parameters.Comments;

[DataContract]
public class UpdatePolicyParameters
{
	[DataMember]
	public bool? AreCommentsEnabled { get; set; }

	[DataMember]
	public CommentModerationPolicy? ModerationPolicy { get; set; }

	[DataMember]
	public bool? OnlyInstructorsCanReply { get; set; }
}