using System.Runtime.Serialization;
using Database.Models;

namespace Ulearn.Web.Api.Models.Responses.Comments
{
	[DataContract]
	public class CommentPolicyResponse
	{
		[DataMember(Name = "are_comments_enabled")]
		public bool AreCommentsEnabled { get; set; }
		
		[DataMember(Name = "moderation")]
		public CommentModerationPolicy ModerationPolicy { get; set; }
		
		[DataMember(Name = "only_instructors_can_reply")]
		public bool OnlyInstructorsCanReply { get; set; }
	}
}