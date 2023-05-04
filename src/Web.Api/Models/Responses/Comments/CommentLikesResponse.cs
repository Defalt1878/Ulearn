using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Models.Responses.Comments;

[DataContract]
public class CommentLikesResponse : PaginatedResponse
{
	[DataMember]
	public List<CommentLikeInfo> Likes { get; set; }
}

[DataContract]
public class CommentLikeInfo
{
	[DataMember]
	public ShortUserInfo User { get; set; }

	[DataMember]
	public DateTime Timestamp { get; set; }
}