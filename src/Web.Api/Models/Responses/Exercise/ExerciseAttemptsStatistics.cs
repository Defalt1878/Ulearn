using System;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Exercise;

[DataContract]
public class ExerciseAttemptsStatistics
{
	[DataMember]
	public int AttemptedUsersCount { get; set; }

	[DataMember]
	public int UsersWithRightAnswerCount { get; set; }

	[DataMember]
	public DateTime? LastSuccessAttemptDate { get; set; }
}