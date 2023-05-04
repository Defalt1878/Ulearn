using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Database.Models;
using JetBrains.Annotations;
using Ulearn.Web.Api.Controllers;
using Ulearn.Web.Api.Models.Common;
using Ulearn.Web.Api.Models.Responses.Review;
using Ulearn.Web.Api.Utils;

namespace Ulearn.Web.Api.Models.Responses.Exercise;

[DataContract]
public class ReviewInfo
{
	[DataMember]
	public int Id { get; set; }

	[CanBeNull]
	[DataMember]
	public ShortUserInfo Author; // null для ulearn bot

	[DataMember]
	public int StartLine;

	[DataMember]
	public int StartPosition;

	[DataMember]
	public int FinishLine;

	[DataMember]
	public int FinishPosition;

	[DataMember]
	public string Comment;

	[DataMember]
	public string RenderedComment;

	[DataMember]
	public DateTime? AddingTime;

	[DataMember]
	public List<ReviewCommentResponse> Comments;

	public static ReviewInfo Build(ExerciseCodeReview r, [CanBeNull] IEnumerable<ExerciseCodeReviewComment> comments, bool isUlearnBot)
	{
		return new ReviewInfo
		{
			Id = r.Id,
			Comment = r.Comment,
			RenderedComment = CommentTextHelper.RenderCommentTextToHtml(r.Comment),
			Author = isUlearnBot ? null : BaseController.BuildShortUserInfo(r.Author),
			AddingTime = isUlearnBot ? null : r.AddingTime,
			FinishLine = r.FinishLine,
			FinishPosition = r.FinishPosition,
			StartLine = r.StartLine,
			StartPosition = r.StartPosition,
			Comments = comments
				.EmptyIfNull()
				.OrderBy(c => c.AddingTime)
				.Select(ReviewCommentResponse.Build)
				.ToList()
		};
	}
}