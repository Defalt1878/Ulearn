﻿using Database.Models;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Courses.Units;

namespace uLearn.Web.Core.Models;

public class StatisticPageModel
{
	public string CourseId { get; set; }
	public string CourseTitle { get; set; }
	public List<Unit> Units { get; set; }
	public List<string> SelectedGroupsIds { get; set; }
	public string SelectedGroupsIdsJoined => string.Join(",", SelectedGroupsIds);
	public List<SingleGroup> Groups { get; set; }
	public Dictionary<int, List<GroupAccess>> GroupsAccesses { get; set; }

	public DateTime PeriodStart { get; set; }
	public DateTime PeriodFinish { get; set; }

	public List<UnitStatisticUserInfo> VisitedUsers { get; set; }
	public bool VisitedUsersIsMore { get; set; }

	public Dictionary<string, List<int>> UsersGroupsIds { get; set; }
	public Dictionary<int, List<string>> EnabledAdditionalScoringGroupsForGroups { get; set; }
}

public class CourseStatisticPageModel : StatisticPageModel
{
	public bool IsInstructor { get; set; }
		
	public bool IsAdmin { get; set; }

	public bool CanViewProfiles { get; set; }

	/* Dictionary<(userId, unitId, scoringGroupId), additionalScore> */
	public DefaultDictionary<Tuple<string, Guid, string>, int> AdditionalScores { get; set; }

	/* Dictionary<(unitId, scoringGroupId), List<Slide>> */
	public DefaultDictionary<Tuple<Guid, string>, List<Slide>> ShouldBeSolvedSlidesByUnitScoringGroup { get; set; }

	public SortedDictionary<string, ScoringGroup> ScoringGroups { get; set; }

	/* Dictionary<(userId, unitId, scoringGroupId), visitScore> */
	public DefaultDictionary<Tuple<string, Guid, string>, int> ScoreByUserUnitScoringGroup { get; set; }

	/* Dictionary<(userId, slideId), visitScore> */
	public DefaultDictionary<Tuple<string, Guid>, int> ScoreByUserAndSlide { get; set; }

	public DefaultDictionary<string, List<int>> VisitedUsersGroups { get; set; }

	public string JsonExportUrl { get; set; }
		
	public string XmlExportUrl { get; set; }
		
	public string XlsxExportUrl { get; set; }

	public List<(string GroupsNames,string Link, bool IsVisibleForStudents)> GroupNamesToGoogleSheetLink { get; set; }

	public SortedDictionary<string, ScoringGroup> GetUsingUnitScoringGroups(Unit unit, SortedDictionary<string, ScoringGroup> courseScoringGroups)
	{
		return unit.Scoring.Groups
			.Where(kv => courseScoringGroups.ContainsKey(kv.Key))
			/* Filter out only scoring groups with should-be-solved slides or with additional scores by instructor */
			.Where(kv => ShouldBeSolvedSlidesByUnitScoringGroup[Tuple.Create(unit.Id, kv.Key)].Count > 0 || kv.Value.CanBeSetByInstructor)
			.ToDictionary(kv => kv.Key, kv => kv.Value)
			.ToSortedDictionary();
	}

	public int GetMaxScoreForUnitByScoringGroup(Unit unit, ScoringGroup scoringGroup)
	{
		var unitScoringGroup = unit.Scoring.Groups.Values.FirstOrDefault(g => g.Id == scoringGroup.Id);
		var maxAdditionalScore = unitScoringGroup != null && unitScoringGroup.CanBeSetByInstructor ? unitScoringGroup.MaxAdditionalScore : 0;
		return unit.GetSlides(false).Where(s => s.ScoringGroup == scoringGroup.Id).Sum(s => s.MaxScore) + maxAdditionalScore;
	}

	public int GetTotalScoreForUserInUnitByScoringGroup(string userId, Unit unit, ScoringGroup scoringGroup)
	{
		return ScoreByUserUnitScoringGroup[Tuple.Create(userId, unit.Id, scoringGroup.Id)] +
				AdditionalScores[Tuple.Create(userId, unit.Id, scoringGroup.Id)];
	}

	public int GetTotalOnlyFullScoreForUserInUnitByScoringGroup(string userId, Unit unit, ScoringGroup scoringGroup)
	{
		var shouldBeSolvedSlides = ShouldBeSolvedSlidesByUnitScoringGroup[Tuple.Create(unit.Id, scoringGroup.Id)];
		var onlyFullScore = 0;
		foreach (var slide in shouldBeSolvedSlides)
		{
			var slideScore = ScoreByUserAndSlide[Tuple.Create(userId, slide.Id)];
			onlyFullScore += GetOnlyFullScore(slideScore, slide);
		}

		return onlyFullScore + AdditionalScores[Tuple.Create(userId, unit.Id, scoringGroup.Id)];
	}

	/* Option "only full scores" acts only on exercise slides.
		If user scores 4 out of 5 points for quiz, it's okay to be scored in course statistics with enabled "only full scores" option.
		Potentially bug: if user is not a member of group with enabled code-review, his slide's max score may vary from slide.MaxScore */
	public int GetOnlyFullScore(int score, Slide slide)
	{
		var isExercise = slide is ExerciseSlide;
		if (!isExercise)
			return score;
		return score == slide.MaxScore ? score : 0;
	}
}

public class UnitSheetPageModel : StatisticPageModel
{
	public Unit Unit { get; set; }
	public List<Slide> Slides { get; set; }
	public Dictionary<Guid, List<Visit>> SlidesVisits { get; set; }

	public Dictionary<string, int> VisitedSlidesCountByUser { get; set; }
	public Dictionary<string, int> VisitedSlidesCountByUserAllTime { get; set; }

	/* Dictionary<(userId, scoringGroupId), additionalScore> */
	public Dictionary<Tuple<string, string>, int> AdditionalScores { get; set; }

	public bool ShowStatisticsLink { get; set; }
}

public class UnitStatModel : UnitSheetPageModel
{
	public int UsersVisitedAllSlidesInPeriodCount { get; set; }
	public Dictionary<Guid, int> QuizzesAverageScore { get; set; }
	public Dictionary<Guid, int> ExercisesSolutionsCount { get; set; }
	public Dictionary<Guid, int> ExercisesAcceptedSolutionsCount { get; set; }
}

public class UnitStatisticUserInfo
{
	public UnitStatisticUserInfo(string userId, string userName, string email, string firstName, string lastName)
	{
		UserId = userId;
		UserName = userName;
		UserEmail = email ?? "";
		UserVisibleName = ApplicationUser.ToVisibleNameWithLastNameFirst(userName, firstName, lastName) ?? "";
		UserFirstName = firstName ?? "";
		UserLastName = lastName ?? "";
	}

	public string UserId { get; private set; }
	public string UserVisibleName { get; private set; }
	public string UserName { get; private set; }
	public string UserFirstName { get; private set; }
	public string UserLastName { get; private set; }
	public string UserEmail { get; private set; }
}

public class UserInfo
{
	public string UserId { get; set; }
	public string UserName { get; set; }
	public UserSlideInfo[] SlidesSlideInfo { get; set; }
}

public class UserSlideInfo
{
	public bool IsVisited { get; set; }
	public bool IsExerciseSolved { get; set; }
	public int AttemptsCount { get; set; }
	public bool IsQuizPassed { get; set; }
	public double QuizPercentage { get; set; }
}