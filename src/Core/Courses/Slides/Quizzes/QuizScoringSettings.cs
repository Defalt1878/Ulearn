using System.Xml.Serialization;

namespace Ulearn.Core.Courses.Slides.Quizzes
{
	[XmlType("scoring")]
	public class QuizScoringSettings : ISlideScoringSettings
	{
		[XmlAttribute("tries")]
		public int MaxTriesCount { get; set; } = 2;

		[XmlAttribute("manualCheck")]
		public bool ManualChecking { get; set; }

		[XmlAttribute("group")]
		public string ScoringGroup { get; set; }
	}
}