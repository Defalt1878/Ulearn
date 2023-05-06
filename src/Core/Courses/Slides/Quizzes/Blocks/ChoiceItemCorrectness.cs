using System.Xml.Serialization;

namespace Ulearn.Core.Courses.Slides.Quizzes.Blocks
{
	public enum ChoiceItemCorrectness
	{
		[XmlEnum("true")]
		True,

		[XmlEnum("false")]
		False,

		[XmlEnum("maybe")]
		Maybe
	}
}