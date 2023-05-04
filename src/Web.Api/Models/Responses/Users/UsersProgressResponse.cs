using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;

namespace Ulearn.Web.Api.Models.Responses.Users;

[DataContract]
public class UsersProgressResponse : SuccessResponse
{
	[DataMember]
	/* Dictionary<userId, *> */
	public Dictionary<string, UserProgress> UserProgress { get; set; }
}

[DataContract]
public class UserProgress
{
	[DataMember]
	/* Dictionary<slideId, *> */
	public Dictionary<Guid, UserProgressSlideResult> VisitedSlides { get; set; }

	[DataMember]
	public Dictionary<Guid, Dictionary<string, int>> AdditionalScores { get; set; }
}

[DataContract]
public class UserProgressSlideResult
{
	[DataMember]
	public int Score { get; set; }

	[DataMember]
	public int UsedAttempts { get; set; }

	[DataMember]
	public bool WaitingForManualChecking { get; set; }

	[DataMember]
	public bool ProhibitFurtherManualChecking { get; set; }

	[DataMember]
	public bool Visited { get; set; }

	[DataMember]
	public DateTime? Timestamp { get; set; }

	[DataMember]
	public bool IsSkipped { get; set; }
}