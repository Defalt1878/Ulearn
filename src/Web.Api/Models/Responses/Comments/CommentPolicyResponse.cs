using System.Runtime.Serialization;
using Database.Models.Comments;
using Ulearn.Common.Api.Models.Responses;

namespace Ulearn.Web.Api.Models.Responses.Comments;

[DataContract]
public class CommentPolicyResponse : SuccessResponse
{
	[DataMember]
	public bool AreCommentsEnabled { get; set; }

	[DataMember]
	public CommentModerationPolicy ModerationPolicy { get; set; }

	[DataMember]
	public bool OnlyInstructorsCanReply { get; set; }
}