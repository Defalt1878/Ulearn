﻿using Database.Models;
using Database.Models.Quizzes;
using JetBrains.Annotations;
using Ulearn.Common;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Slides.Quizzes;

namespace uLearn.Web.Core.Models;

public class QuizModel
{
	public Course Course { get; set; }

	public QuizSlide Slide { get; set; }
	public QuizState QuizState { get; set; }

	[CanBeNull]
	public Dictionary<string, List<UserQuizAnswer>> AnswersToQuizzes { get; set; }

	[CanBeNull]
	public Dictionary<string, int> UserScores { get; set; }

	/* (quizId -> (itemId -> frequency%)) */
	public DefaultDictionary<string, DefaultDictionary<string, int>> QuestionAnswersFrequency { get; set; } = new();

	public int MaxAttemptsCount { get; set; }
	public bool IsLti { get; set; }
	public bool IsGuest { get; set; }
	
	public bool IsInstructor { get; set; }
	
	public bool IsCourseAdmin { get; set; }

	[CanBeNull]
	public ManualQuizChecking Checking { get; set; }

	public int ManualCheckingsLeftInQueue { get; set; }
	public bool CanUserFillQuiz { get; set; }

	/* GroupsIds != null if instructor filtered users by group and see their works */
	public List<string> GroupsIds { get; set; }

	public string GroupsIdsJoined => string.Join(",", GroupsIds ?? new List<string>());

	public string BaseUrlWeb { get; set; }

	public string BaseUrlApi { get; set; }

	public int Score
	{
		get { return UserScores?.AsEnumerable().Sum(res => res.Value) ?? 0; }
	}

	public int QuestionsCount => UserScores?.Count ?? 0;

	public string IsLtiToString => IsLti.ToString().ToLowerInvariant();

	public bool IsManualCheckingEnabledForUser;
}