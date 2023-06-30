using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Models.Responses.Comments
{
	[DataContract]
	public class CommentsListResponse : PaginatedResponse
	{
		[DataMember]
		public List<CommentResponse> TopLevelComments { get; set; }
	}

	[DataContract]
	public class CommentResponse
	{
		[DataMember]
		public int Id { get; set; }

		[DataMember]
		public ShortUserInfo Author { get; set; }

		[DataMember]
		public List<ShortGroupInfo> AuthorGroups { get; set; }

		[DataMember]
		public string Text { get; set; }

		[DataMember]
		public string RenderedText { get; set; }

		[DataMember]
		public DateTime PublishTime { get; set; }

		[DataMember]
		public bool IsApproved { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public bool? IsCorrectAnswer { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public bool? IsPinnedToTop { get; set; }

		[DataMember]
		public bool IsLiked { get; set; }

		[DataMember]
		public int LikesCount { get; set; }

		[DataMember]
		public List<CommentResponse> Replies { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public string CourseId { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public Guid? SlideId { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public int? ParentCommentId { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public bool IsPassed { get; set; }
	}
}