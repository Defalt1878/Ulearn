using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using JetBrains.Annotations;
using Ulearn.Common.Extensions;

namespace Ulearn.Core.Courses
{
	public class ScoringSettings
	{
		public const string VisitsGroupId = "visits";

		public ScoringSettings()
		{
			_groups = Array.Empty<ScoringGroup>();
		}

		[XmlAttribute("defaultQuiz")]
		public string defaultScoringGroupForQuiz { get; set; }

		[XmlIgnore]
		public string DefaultScoringGroupForQuiz =>
			string.IsNullOrEmpty(defaultScoringGroupForQuiz) ? DefaultScoringGroup : defaultScoringGroupForQuiz;

		[XmlAttribute("defaultExercise")]
		public string defaultScoringGroupForExercise { get; set; }

		[XmlIgnore]
		public string DefaultScoringGroupForExercise =>
			string.IsNullOrEmpty(defaultScoringGroupForExercise) ? DefaultScoringGroup : defaultScoringGroupForExercise;

		[XmlAttribute("default")]
		public string DefaultScoringGroup { get; set; } = "";

		[XmlElement("group")]
		[NotNull]
		public ScoringGroup[] _groups { get; set; }

		private SortedDictionary<string, ScoringGroup> groupsCache;

		[XmlIgnore]
		[NotNull]
		public SortedDictionary<string, ScoringGroup> Groups
		{
			get
			{
				return groupsCache ??= _groups
					.Where(g => g.Id != VisitsGroupId)
					.ToDictionary(g => g.Id, g => g)
					.ToSortedDictionary();
			}
		}

		[CanBeNull]
		public ScoringGroup VisitsGroup
		{
			get { return _groups.FirstOrDefault(g => g.Id == VisitsGroupId); }
		}

		public void CopySettingsFrom(ScoringSettings otherScoringSettings)
		{
			defaultScoringGroupForQuiz = string.IsNullOrEmpty(defaultScoringGroupForQuiz) && string.IsNullOrEmpty(DefaultScoringGroup)
				? otherScoringSettings.DefaultScoringGroupForQuiz
				: defaultScoringGroupForQuiz;
			defaultScoringGroupForExercise = string.IsNullOrEmpty(defaultScoringGroupForExercise) && string.IsNullOrEmpty(DefaultScoringGroup)
				? otherScoringSettings.DefaultScoringGroupForExercise
				: defaultScoringGroupForExercise;
			DefaultScoringGroup = string.IsNullOrEmpty(DefaultScoringGroup) ? otherScoringSettings.DefaultScoringGroup : DefaultScoringGroup;

			/* Copy missing scoring groups */
			foreach (var scoringGroupId in otherScoringSettings.Groups.Keys)
				if (!Groups.ContainsKey(scoringGroupId))
					Groups[scoringGroupId] = otherScoringSettings.Groups[scoringGroupId];
		}

		public int GetMaxAdditionalScore()
		{
			return Groups.Values.Where(g => g.CanBeSetByInstructor).Sum(g => g.MaxAdditionalScore);
		}
	}
}