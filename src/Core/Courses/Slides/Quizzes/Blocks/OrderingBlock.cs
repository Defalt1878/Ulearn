using System.Linq;
using System.Xml.Serialization;

namespace Ulearn.Core.Courses.Slides.Quizzes.Blocks
{
	[XmlType("question.order")]
	public class OrderingBlock : AbstractQuestionBlock
	{
		[XmlElement("item")]
		public OrderingItem[] Items;

		[XmlAttribute("explanation")]
		public string Explanation;

		public OrderingItem[] ShuffledItems()
		{
			return Items.Shuffle().ToArray();
		}

		public override bool HasEqualStructureWith(SlideBlock other)
		{
			return other is OrderingBlock block &&
					Items.SequenceEqual(block.Items);
		}
	}
}