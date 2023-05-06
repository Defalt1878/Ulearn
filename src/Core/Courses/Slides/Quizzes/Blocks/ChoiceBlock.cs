using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Serialization;

namespace Ulearn.Core.Courses.Slides.Quizzes.Blocks
{
	[XmlType("question.choice")]
	public class ChoiceBlock : AbstractQuestionBlock
	{
		[XmlAttribute("multiple")]
		public bool Multiple;

		[XmlAttribute("shuffle")]
		public bool Shuffle;

		[XmlElement("item")]
		public ChoiceItem[] Items;

		[XmlElement("allowedMistakes")]
		public MistakesCount AllowedMistakesCount { get; set; } = new(0, 0);

		public ChoiceItem[] ShuffledItems()
		{
			return Shuffle ? Items.Shuffle().ToArray() : Items;
		}

		public override void Validate(SlideBuildingContext slideBuildingContext)
		{
			if (Items.Deprecated_DistinctBy(i => i.Id).Count() != Items.Length)
				throw new FormatException("Duplicate choice id in quizBlock " + Id);
			if (!Multiple && Items.Count(i => i.IsCorrect == ChoiceItemCorrectness.True) != 1)
				throw new FormatException("Should be exactly one correct item for non-multiple choice. BlockId=" + Id);
			if (!Multiple && Items.Any(i => i.IsCorrect == ChoiceItemCorrectness.Maybe))
				throw new FormatException("'Maybe' items are not allowed for for non-multiple choice. BlockId=" + Id);
		}

		public override string TryGetText()
		{
			return Text + '\n' + string.Join("\n", Items.Select(item => item.GetText()));
		}

		public override bool HasEqualStructureWith(SlideBlock other)
		{
			if (other is not ChoiceBlock block)
				return false;
			if (Items.Length != block.Items.Length || Multiple != block.Multiple)
				return false;

			var idsSet = Items.Select(i => i.Id).ToImmutableHashSet();
			var blockIdsSet = block.Items.Select(i => i.Id).ToImmutableHashSet();
			return idsSet.SetEquals(blockIdsSet);
		}
	}
}