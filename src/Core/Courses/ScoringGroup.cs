using System.Xml.Serialization;

namespace Ulearn.Core.Courses
{
	public class ScoringGroup
	{
		private const int defaultMaxAdditionalScore = 0;
		private const bool defaultEnabledForEveryone = false;

		[XmlAttribute("enableForEveryone")]
		public string enabledForEveryone;

		[XmlAttribute("id")]
		public string Id { get; set; }

		[XmlAttribute("abbr")]
		public string Abbreviation { get; set; }

		[XmlAttribute("description")]
		public string Description { get; set; }

		[XmlAttribute("weight")]
		public decimal Weight { get; set; } = 1;

		[XmlIgnore]
		public bool CanBeSetByInstructor => MaxAdditionalScore > 0;

		[XmlAttribute("maxAdditionalScore")]
		public string maxAdditionalScore { get; set; }

		[XmlIgnore]
		public int MaxAdditionalScore
		{
			get
			{
				if (string.IsNullOrEmpty(maxAdditionalScore) || maxAdditionalScore.Trim().Length == 0)
					return defaultMaxAdditionalScore;

				return int.TryParse(maxAdditionalScore, out var result) ? result : defaultMaxAdditionalScore;
			}
			set => maxAdditionalScore = value.ToString();
		}

		[XmlIgnore]
		public bool IsMaxAdditionalScoreSpecified => !string.IsNullOrEmpty(maxAdditionalScore);

		[XmlIgnore]
		/* Calculates automatically by slides's scores */
		// Считается только по нескрытым слайдам
		public int MaxNotAdditionalScore { get; set; }

		[XmlIgnore]
		public bool EnabledForEveryone
		{
			get
			{
				if (string.IsNullOrEmpty(enabledForEveryone) || enabledForEveryone.Trim().Length == 0)
					return defaultEnabledForEveryone;

				return bool.TryParse(enabledForEveryone, out var value) && value;
			}
			set => enabledForEveryone = value.ToString();
		}

		[XmlIgnore]
		public bool IsEnabledForEveryoneSpecified => !string.IsNullOrEmpty(enabledForEveryone);

		[XmlText]
		public string Name { get; set; }

		public void CopySettingsFrom(ScoringGroup otherScoringGroup)
		{
			maxAdditionalScore = string.IsNullOrEmpty(maxAdditionalScore) ? otherScoringGroup.maxAdditionalScore : maxAdditionalScore;
			enabledForEveryone = string.IsNullOrEmpty(enabledForEveryone) ? otherScoringGroup.enabledForEveryone : enabledForEveryone;
			Abbreviation ??= otherScoringGroup.Abbreviation;
			Name = string.IsNullOrEmpty(Name) ? otherScoringGroup.Name : Name;
			Description = string.IsNullOrEmpty(Description) ? otherScoringGroup.Description : Description;
		}
	}
}