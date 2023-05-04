using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Database.Models.Comments;
using Ulearn.Common.Api.Models.Validations;

namespace Ulearn.Web.Api.Models.Parameters.Comments
{
	[DataContract]
	public class CreateCommentParameters
	{
		[DataMember(IsRequired = true)]
		public Guid SlideId { get; set; }

		[DataMember(IsRequired = true)]
		[NotEmpty(ErrorMessage = "Text can not be empty")]
		[MaxLength(CommentsPolicy.MaxCommentLength, ErrorMessage = "Comment is too large. Max allowed length is 10000 chars")]
		public string Text { get; set; }

		/// <summary>
		///     Id комментария, на который это ответ
		/// </summary>
		[DataMember]
		public int? ParentCommentId { get; set; }

		[DataMember]
		public bool ForInstructors { get; set; }
	}
}