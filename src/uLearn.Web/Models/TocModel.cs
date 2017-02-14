﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace uLearn.Web.Models
{
	public class TocModel
	{
		public TocModel(Course course, TocUnitModel[] units)
		{
			Course = course;
			Units = units;
		}

		public readonly Course Course;
		public readonly TocUnitModel[] Units;
		public int MaxScore { get { return Units.Sum(u => u.MaxScore); } }
		public int Score { get { return Units.Sum(p => p.Score); } }
		public DateTime NextUnitTime;
	}

	public class TocUnitModel
	{
		public string UnitName;
		public List<TocPageInfo> Pages;
		public bool IsCurrent;
		public Dictionary<ScoringGroup, int> AdditionalScores;
		public int Score => Pages.Sum(p => p.Score) + AdditionalScores.Values.Sum();
		public int MaxScore => Pages.Sum(p => p.MaxScore) + AdditionalScores.Keys.Sum(g => g.MaxAdditionalScore);

	}


	public class TocPageInfo
	{
		public Guid SlideId;
		public string Url;
		public string Name;
		public bool ShouldBeSolved;
		public int MaxScore;
		public int Score;
		public bool IsCurrent;
		public TocPageType PageType;
		public bool IsSolved;
		public bool IsVisited;
	}

	public enum TocPageType
	{
		Theory,
		Exercise,
		Quiz,
		InstructorNotes,
		Statistics
	}
}