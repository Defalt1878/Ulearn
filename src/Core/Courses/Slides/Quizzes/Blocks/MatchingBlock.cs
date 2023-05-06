using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Serialization;

namespace Ulearn.Core.Courses.Slides.Quizzes.Blocks
{
	[XmlType("question.match")]
	public class MatchingBlock : AbstractQuestionBlock
	{
		[XmlAttribute("shuffleFixed")]
		public bool ShuffleFixed;

		[XmlElement("explanation")]
		public string Explanation;

		[XmlElement("match")]
		public MatchingMatch[] Matches;

		private readonly Random random = new();

		public List<MatchingMatch> GetMatches(bool shuffle = false)
		{
			return shuffle
				? Matches.Shuffle(random).ToList()
				: Matches.ToList();
		}

		public override bool HasEqualStructureWith(SlideBlock other)
		{
			if (other is not MatchingBlock block)
				return false;
			if (Matches.Length != block.Matches.Length)
				return false;
			var idsSet = Matches.Select(m => m.Id).ToImmutableHashSet();
			var blockIdsSet = block.Matches.Select(m => m.Id).ToImmutableHashSet();
			return idsSet.SetEquals(blockIdsSet);
		}
	}
}