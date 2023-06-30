using System.Runtime.Serialization;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Models.Responses.CodeReview;

public class ReviewComment
{
	[DataMember]
	public int CommentId { get; set; }

	[DataMember]
	public ShortUserInfo Author { get; set; }

	[DataMember]
	public string CodeFragment { get; set; }

	[DataMember]
	public string Comment { get; set; }
}