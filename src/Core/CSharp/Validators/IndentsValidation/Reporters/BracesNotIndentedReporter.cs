using System.Collections.Generic;
using System.Linq;

namespace Ulearn.Core.CSharp.Validators.IndentsValidation.Reporters
{
	internal static class BracesNotIndentedReporter
	{
		public static IEnumerable<SolutionStyleError> Report(IEnumerable<BracesPair> bracesPairs)
		{
			foreach (var braces in bracesPairs.Where(pair => pair.Open.GetLine() != pair.Close.GetLine() &&
															Indent.TokenIsFirstAtLine(pair.Open)))
			{
				var correctOpenBraceParent = braces.Open.GetFirstTokenOfCorrectOpenBraceParent();
				if (correctOpenBraceParent == default)
					continue;
				var parentLineIndent = new Indent(correctOpenBraceParent);
				var openBraceLineIndent = new Indent(braces.Open);
				if (openBraceLineIndent.LengthInSpaces < parentLineIndent.LengthInSpaces)
					yield return new SolutionStyleError(StyleErrorType.Indents04, braces.Open, braces);
			}
		}
	}
}