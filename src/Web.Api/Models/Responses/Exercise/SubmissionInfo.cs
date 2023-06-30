using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Database.Models;
using JetBrains.Annotations;
using Ulearn.Common;

namespace Ulearn.Web.Api.Models.Responses.Exercise
{
	[DataContract]
	public class SubmissionInfo
	{
		[DataMember]
		public int Id;

		[DataMember]
		public string Code = null!;

		[DataMember]
		public Language Language;

		[DataMember]
		public DateTime Timestamp;

		[DataMember]
		public ExerciseAutomaticCheckingResponse? AutomaticChecking; // null если задача не имеет автоматических тестов, это не отменяет возможности ручной проверки.

		[DataMember]
		public ExerciseManualCheckingResponse? ManualChecking; // null, если у submission нет ManualExerciseChecking

		public static SubmissionInfo Build(
			UserExerciseSubmission submission,
			Dictionary<int, IEnumerable<ExerciseCodeReviewComment>>? reviewId2Comments,
			bool showCheckerLogs)
		{
			var botReviews = submission.NotDeletedReviews
				.Select(r => ToReviewInfo(r, true, reviewId2Comments))
				.ToList();
			var manualCheckingReviews = (submission.ManualChecking?.NotDeletedReviews).EmptyIfNull()
				.Select(r => ToReviewInfo(r, false, reviewId2Comments))
				.ToList();
			var automaticChecking = submission.AutomaticChecking == null
				? null : ExerciseAutomaticCheckingResponse.Build(submission.AutomaticChecking, botReviews, showCheckerLogs);
			var manualChecking = submission.ManualChecking == null ? null : ExerciseManualCheckingResponse.Build(submission.ManualChecking, manualCheckingReviews);
			return new SubmissionInfo
			{
				Id = submission.Id,
				Code = submission.SolutionCode.Text,
				Language = submission.Language,
				Timestamp = submission.Timestamp,
				AutomaticChecking = automaticChecking,
				ManualChecking = manualChecking
			};
		}

		private static ReviewInfo ToReviewInfo(ExerciseCodeReview r, bool isUlearnBot,
			Dictionary<int, IEnumerable<ExerciseCodeReviewComment>>? reviewId2Comments)
		{
			var comments = reviewId2Comments?.GetValueOrDefault(r.Id);
			return ReviewInfo.Build(r, comments, isUlearnBot);
		}
	}
}