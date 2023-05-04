using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Database.Models;
using JetBrains.Annotations;
using Ulearn.Common;

// ReSharper disable NotNullOrRequiredMemberIsNotInitialized

namespace Ulearn.Web.Api.Models.Responses.Exercise
{
	[DataContract]
	public class SubmissionInfo
	{
		[DataMember]
		public int Id;

		[NotNull]
		[DataMember]
		public string Code;

		[DataMember]
		public Language Language;

		[DataMember]
		public DateTime Timestamp;

		[CanBeNull]
		[DataMember]
		public ExerciseAutomaticCheckingResponse AutomaticChecking; // null если задача не имеет автоматических тестов, это не отменяет возможности ручной проверки.

		[CanBeNull]
		[DataMember]
		public ExerciseManualCheckingResponse ManualChecking; // null, если у submission нет ManualExerciseChecking

		public static SubmissionInfo Build(
			UserExerciseSubmission submission,
			[CanBeNull] Dictionary<int, IEnumerable<ExerciseCodeReviewComment>> reviewId2Comments,
			bool showCheckerLogs)
		{
			var botReviews = submission.NotDeletedReviews
				.Select(r => ToReviewInfo(r, true, reviewId2Comments))
				.ToList();
			var manualCheckingReviews = (submission.ManualChecking?.NotDeletedReviews).EmptyIfNull()
				.Select(r => ToReviewInfo(r, false, reviewId2Comments))
				.ToList();
			var automaticChecking = submission.AutomaticChecking is null
				? null : ExerciseAutomaticCheckingResponse.Build(submission.AutomaticChecking, botReviews, showCheckerLogs);
			var manualChecking = submission.ManualChecking is null ? null : ExerciseManualCheckingResponse.Build(submission.ManualChecking, manualCheckingReviews);
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
			[CanBeNull] Dictionary<int, IEnumerable<ExerciseCodeReviewComment>> reviewId2Comments)
		{
			var comments = reviewId2Comments?.GetValueOrDefault(r.Id);
			return ReviewInfo.Build(r, comments, isUlearnBot);
		}
	}
}