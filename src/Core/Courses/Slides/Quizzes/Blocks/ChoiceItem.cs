using System.Xml.Serialization;

namespace Ulearn.Core.Courses.Slides.Quizzes.Blocks
{
	public class ChoiceItem
	{
		[XmlAttribute("id")]
		public string Id;

		[XmlAttribute("isCorrect")]
		public ChoiceItemCorrectness IsCorrect = ChoiceItemCorrectness.False;

		[XmlAttribute("explanation")]
		public string explanation;

		[XmlIgnore]
		public string Explanation
		{
			get
			{
				if (!string.IsNullOrEmpty(explanation))
					return explanation;
				return IsCorrect is ChoiceItemCorrectness.Maybe
					? "Это опциональный вариант: его можно было как выбрать, так и не выбирать"
					: "";
			}
			set => explanation = value;
		}

		[XmlText]
		public string Description;

		public string GetText()
		{
			return (Description ?? "") + '\t' + (Explanation ?? "");
		}
	}
}