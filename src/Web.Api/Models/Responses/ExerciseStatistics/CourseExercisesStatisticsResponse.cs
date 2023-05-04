using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Models.Responses.ExerciseStatistics;

[DataContract]
public class CourseExercisesStatisticsResponse : SuccessResponse
{
	[DataMember]
	public List<OneExerciseStatistics> Exercises { get; set; }

	[DataMember]
	public int AnalyzedSubmissionsCount { get; set; }
}

[DataContract]
public class OneExerciseStatistics
{
	[DataMember]
	public ShortSlideInfo Exercise { get; set; }

	[DataMember]
	public int SubmissionsCount { get; set; }

	[DataMember]
	public int AcceptedCount { get; set; }

	[DataMember]
	public Dictionary<DateTime, OneExerciseStatisticsForDate> LastDates { get; set; }
}

[DataContract]
public class OneExerciseStatisticsForDate
{
	[DataMember]
	public int SubmissionsCount { get; set; }

	[DataMember]
	public int AcceptedCount { get; set; }
}