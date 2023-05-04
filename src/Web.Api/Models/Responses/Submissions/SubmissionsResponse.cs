using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Database.Models;
using JetBrains.Annotations;
using Ulearn.Web.Api.Models.Responses.Exercise;

namespace Ulearn.Web.Api.Models.Responses.Submissions
{
	[DataContract]
	public class SubmissionsResponse
	{
		[DataMember]
		public List<SubmissionInfo> Submissions { get; set; }

		[DataMember]
		public bool ProhibitFurtherManualChecking { get; set; }

		public static SubmissionsResponse Build(
			IEnumerable<UserExerciseSubmission> submissions,
			[CanBeNull] Dictionary<int, IEnumerable<ExerciseCodeReviewComment>> reviewId2Comments,
			bool showCheckerLogs = false,
			bool prohibitFurtherManualChecking = true
		)
		{
			return new SubmissionsResponse
			{
				Submissions = submissions.Select(s => SubmissionInfo.Build(s, reviewId2Comments, showCheckerLogs)).ToList(),
				ProhibitFurtherManualChecking = prohibitFurtherManualChecking
			};
		}
	}
}