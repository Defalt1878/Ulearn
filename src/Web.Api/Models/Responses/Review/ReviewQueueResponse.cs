using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Database.Models;
using Ulearn.Common.Api.Models.Responses;

namespace Ulearn.Web.Api.Models.Responses.Review;

[DataContract]
public class ReviewQueueResponse : SuccessResponse
{
	[DataMember]
	public List<ReviewQueueItem> Checkings { get; set; }

	public static ReviewQueueResponse Build(IEnumerable<AbstractManualSlideChecking> checkings)
	{
		return new ReviewQueueResponse
		{
			Checkings = checkings
				.Select(c => new ReviewQueueItem
				{
					SubmissionId = c.Id,
					SlideId = c.SlideId,
					UserId = c.UserId,
					IsLocked = c.IsLocked,
					Type = c is ManualExerciseChecking
						? QueueItemType.Exercise
						: QueueItemType.Quiz
				})
				.ToList()
		};
	}
}

[DataContract]
public class ReviewQueueItem
{
	[DataMember]
	public int SubmissionId { get; set; } // ExerciseSubmission id or QuizSubmission id

	[DataMember]
	public Guid SlideId { get; set; }

	[DataMember]
	public string UserId { get; set; }

	[DataMember]
	public bool IsLocked { get; set; }

	[DataMember]
	public QueueItemType Type { get; set; }
}

public enum QueueItemType
{
	Exercise,
	Quiz
}