using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Models.Responses.CodeReviewStatistics;

[DataContract]
public class CodeReviewInstructorsStatisticsResponse : SuccessResponse
{
	[DataMember]
	public List<CodeReviewInstructorStatistics> Instructors { get; set; }

	[DataMember]
	public int AnalyzedCodeReviewsCount { get; set; }
}

[DataContract]
public class CodeReviewInstructorStatistics
{
	[DataMember]
	public ShortUserInfo Instructor { get; set; }

	[DataMember]
	public List<CodeReviewExerciseStatistics> Exercises { get; set; }
}

[DataContract]
public class CodeReviewExerciseStatistics
{
	[DataMember]
	public Guid SlideId { get; set; }

	[DataMember]
	public int ReviewedSubmissionsCount { get; set; }

	[DataMember]
	public int QueueSize { get; set; }

	[DataMember]
	public int CommentsCount { get; set; }
}