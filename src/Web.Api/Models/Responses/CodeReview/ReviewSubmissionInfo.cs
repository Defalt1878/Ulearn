using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Models.Responses.CodeReview;

public class ReviewSubmissionInfo
{
	[DataMember]
	public int SubmissionId { get; set; }

	[DataMember]
	public string CourseId { get; set; } = null!;
	
	[DataMember]
	public Guid SlideId { get; set; }
	
	[DataMember]
	public DateTime Timestamp { get; set; }

	[DataMember]
	public ShortUserInfo User { get; set; } = null!;
	
	[DataMember]
	public int Score { get; set; }
	
	[DataMember]
	public int MaxScore { get; set; }
	
	[DataMember]
	public ShortUserInfo? LockedBy { get; set; }
	
	[DataMember]
	public DateTime? LockedUntil { get; set; }

	[DataMember]
	public List<ReviewComment>? Reviews { get; set; }
}