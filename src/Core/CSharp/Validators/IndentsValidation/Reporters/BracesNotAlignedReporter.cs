using System.Collections.Generic;
using System.Linq;

namespace Ulearn.Core.CSharp.Validators.IndentsValidation.Reporters
{
	internal static class BracesNotAlignedReporter
	{
		public static IEnumerable<SolutionStyleError> Report(IEnumerable<BracesPair> bracesPairs)
		{
			foreach (var braces in bracesPairs.Where(pair => pair.Open.GetLine() != pair.Close.GetLine()))
			{
				var openBraceIndent = new Indent(braces.Open);
				var closeBraceIndent = new Indent(braces.Close);
				if (openBraceIndent.IndentedTokenIsFirstAtLine && openBraceIndent.LengthInSpaces != closeBraceIndent.LengthInSpaces)
					yield return new SolutionStyleError(StyleErrorType.Indents03, braces.Open, braces);
			}
		}
	}
}