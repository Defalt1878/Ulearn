using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Database.Models;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Units;
using Ulearn.Web.Api.Models.Internal;

namespace Ulearn.Web.Api.Models.Responses.Analytics;

[DataContract]
public class CourseStatisticsModel
{
	[DataMember(Name = "course")]
	public CourseStatisticsCourseInfo Course;

	[DataMember(Name = "groups")]
	public CourseStatisticsGroupInfo[] Groups;

	[DataMember(Name = "students")]
	public CourseStatisticsStudentInfo[] Students;

	public CourseStatisticsModel()
	{
	}

	public CourseStatisticsModel(CourseStatisticPageModel model)
	{
		Course = new CourseStatisticsCourseInfo(model.CourseTitle, model.Units, model.ScoringGroups.Select(kvp => kvp.Value));
		Groups = model.Groups.Select(g => new CourseStatisticsGroupInfo(g)).ToArray();

		var students = new List<CourseStatisticsStudentInfo>();
		var shouldBeSolvedSlides = model.Units.SelectMany(u => u.GetSlides(false)).Where(s => s.ShouldBeSolved).ToList();
		foreach (var user in model.VisitedUsers)
		{
			var studentInfo = new CourseStatisticsStudentInfo
			{
				UserId = user.UserId,
				VisibleName = user.UserVisibleName,
				GroupsIds = model.VisitedUsersGroups[user.UserId].ToArray(),
				SlidesScores = shouldBeSolvedSlides.Select(s => new CourseStatisticsStudentSlideScore
				{
					SlideId = s.Id,
					Score = model.ScoreByUserAndSlide[Tuple.Create(user.UserId, s.Id)]
				}).ToArray(),
				AdditionalScores = model.Units
					.SelectMany(
						u => u.Scoring.Groups.Values.Where(g => g.CanBeSetByInstructor).Select(g => new CourseStatisticsStudentAdditionalScore
						{
							UnitId = u.Id,
							ScoringGroupId = g.Id,
							Score = model.AdditionalScores[Tuple.Create(user.UserId, u.Id, g.Id)]
						})
					)
					.ToArray()
			};
			students.Add(studentInfo);
		}

		Students = students.ToArray();
	}
}

[DataContract(Name = "course")]
public class CourseStatisticsCourseInfo
{
	[DataMember(Name = "title")]
	public string Title;

	[DataMember(Name = "units")]
	public CourseStatisticsUnitInfo[] Units;

	[DataMember(Name = "scoring_groups")]
	public CourseStatisticsScoringGroupInfo[] ScoringGroups;

	public CourseStatisticsCourseInfo()
	{
	}

	public CourseStatisticsCourseInfo(string courseTitle, IEnumerable<Unit> units, IEnumerable<ScoringGroup> scoringGroups)
	{
		Title = courseTitle;
		Units = units.Select(u => new CourseStatisticsUnitInfo(u)).ToArray();
		ScoringGroups = scoringGroups.Select(g => new CourseStatisticsScoringGroupInfo(g)).ToArray();
	}
}

[DataContract(Name = "scoring_group")]
public class CourseStatisticsScoringGroupInfo
{
	[DataMember(Name = "id")]
	public string Id;

	[DataMember(Name = "abbreviation")]
	public string Abbreviation;

	[DataMember(Name = "name")]
	public string Name;

	public CourseStatisticsScoringGroupInfo()
	{
	}

	public CourseStatisticsScoringGroupInfo(ScoringGroup group)
	{
		Id = group.Id;
		Abbreviation = group.Abbreviation;
		Name = group.Name;
	}
}

[DataContract(Name = "unit")]
public class CourseStatisticsUnitInfo
{
	[DataMember(Name = "id")]
	public Guid Id;

	[DataMember(Name = "title")]
	public string Title;

	[DataMember(Name = "slides")]
	public CourseStatisticsSlideInfo[] Slides;

	[DataMember(Name = "additional_scores")]
	public CourseStatisticsUnitAdditionalScoreInfo[] AdditionalScores;

	public CourseStatisticsUnitInfo()
	{
	}

	public CourseStatisticsUnitInfo(Unit unit)
	{
		Id = unit.Id;
		Title = unit.Title;
		Slides = unit.GetSlides(false).Where(s => s.ShouldBeSolved).Select(s => new CourseStatisticsSlideInfo(s)).ToArray();
		AdditionalScores = unit.Scoring.Groups.Values.Where(g => g.CanBeSetByInstructor).Select(g => new CourseStatisticsUnitAdditionalScoreInfo(g)).ToArray();
	}
}

[DataContract(Name = "additional_score")]
public class CourseStatisticsUnitAdditionalScoreInfo
{
	[DataMember(Name = "scoring_group_id")]
	public string ScoringGroupId;

	[DataMember(Name = "max_additional_score")]
	public int MaxAdditionalScore;

	public CourseStatisticsUnitAdditionalScoreInfo()
	{
	}

	public CourseStatisticsUnitAdditionalScoreInfo(ScoringGroup g)
	{
		ScoringGroupId = g.Id;
		MaxAdditionalScore = g.MaxAdditionalScore;
	}
}

[DataContract(Name = "slide")]
public class CourseStatisticsSlideInfo
{
	[DataMember(Name = "id")]
	public Guid Id;

	[DataMember(Name = "title")]
	public string Title;

	[DataMember(Name = "max_score")]
	public int MaxScore;

	public CourseStatisticsSlideInfo()
	{
	}

	public CourseStatisticsSlideInfo(Slide s)
	{
		Id = s.Id;
		Title = s.Title;
		MaxScore = s.MaxScore;
	}
}

[DataContract(Name = "group")]
public class CourseStatisticsGroupInfo
{
	[DataMember(Name = "id")]
	public int Id;

	[DataMember(Name = "Title")]
	public string Title;

	public CourseStatisticsGroupInfo()
	{
	}

	public CourseStatisticsGroupInfo(SingleGroup g)
	{
		Id = g.Id;
		Title = g.Name;
	}
}

[DataContract(Name = "info")]
public class CourseStatisticsStudentInfo
{
	[DataMember(Name = "user_id")]
	public string UserId;

	[DataMember(Name = "groups")]
	public int[] GroupsIds;

	[DataMember(Name = "name")]
	public string VisibleName;

	[DataMember(Name = "slides_scores")]
	public CourseStatisticsStudentSlideScore[] SlidesScores;

	[DataMember(Name = "additional_scores")]
	public CourseStatisticsStudentAdditionalScore[] AdditionalScores;
}

[DataContract(Name = "slide_score")]
public class CourseStatisticsStudentSlideScore
{
	[DataMember(Name = "slide_id")]
	public Guid SlideId;

	[DataMember(Name = "score")]
	public int Score;
}

[DataContract(Name = "additional_score")]
public class CourseStatisticsStudentAdditionalScore
{
	[DataMember(Name = "unit_id")]
	public Guid UnitId;

	[DataMember(Name = "scoring_group_id")]
	public string ScoringGroupId;

	[DataMember(Name = "score")]
	public int Score;
}