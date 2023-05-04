using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Database.Models.Comments;
using Ulearn.Common.Api.Models.Validations;

namespace Ulearn.Web.Api.Models.Parameters.Review;

[DataContract]
public class ReviewCreateCommentParameters
{
	[DataMember(IsRequired = true)]
	[NotEmpty(ErrorMessage = "Text can not be empty")]
	[MaxLength(CommentsPolicy.MaxCommentLength, ErrorMessage = "Comment is too large. Max allowed length is 10000 chars")]
	public string Text { get; set; }
}