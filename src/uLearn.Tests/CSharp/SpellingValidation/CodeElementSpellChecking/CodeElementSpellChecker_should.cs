using NUnit.Framework;
using Ulearn.Core.CSharp.Validators.SpellingValidator;

namespace uLearn.CSharp.SpellingValidation.CodeElementSpellChecking
{
	[TestFixture]
	public class CodeElementSpellChecker_should
	{
		private readonly CodeElementSpellChecker spellChecker = new();

		[TestCase("wildWildWest", ExpectedResult = false)]
		[TestCase("Filename", ExpectedResult = false)]
		[TestCase("Heatmap", ExpectedResult = false)]
		[TestCase("Heatmapdata", ExpectedResult = false)]
		[TestCase("wildWildEwst", ExpectedResult = true)]
		[TestCase("filenaem", ExpectedResult = false)] // разбивается на f, ile, na, em, которые (не беря во внимание f) есть в латинском словаре
		[TestCase("sw", ExpectedResult = false)]
		[TestCase("stri", "String", ExpectedResult = false)]
		[TestCase("csl", "CSharpLanguage", ExpectedResult = false)]
		[TestCase("Heapify", "CSharpLanguage", ExpectedResult = false)]
		[TestCase("strings", "CSharpLanguage", ExpectedResult = false)]
		[TestCase("xlabel", "CSharpLanguage", ExpectedResult = false)]
		public bool Test_CheckIdentifierNameForSpellingErrors(string identifierName, string type = null)
		{
			return spellChecker.CheckIdentifierNameForSpellingErrors(identifierName, type);
		}
	}
}