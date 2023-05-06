using System;
using System.Xml.Serialization;

namespace Ulearn.Core.Courses.Slides.Quizzes.Blocks
{
	[XmlType("question.isTrue")]
	public class IsTrueBlock : AbstractQuestionBlock
	{
		[XmlAttribute("answer")]
		public bool Answer;

		[XmlAttribute("explanation")]
		public string Explanation;

		public bool IsRight(string text)
		{
			return string.Equals(text, Answer.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		public override void Validate(SlideBuildingContext slideBuildingContext)
		{
		}

		public override string TryGetText()
		{
			return Text + '\n' + Explanation;
		}

		public override bool HasEqualStructureWith(SlideBlock other)
		{
			return other is IsTrueBlock block && Answer == block.Answer;
		}
	}
}