using System.Collections.Generic;
using System.Linq;

namespace Ulearn.Core.CSharp.Validators.IndentsValidation.Reporters
{
	internal static class CloseBraceHasCodeOnSameLineReporter
	{
		public static IEnumerable<SolutionStyleError> Report(IEnumerable<BracesPair> bracesPairs)
		{
			foreach (var braces in bracesPairs.Where(pair => pair.Open.GetLine() != pair.Close.GetLine()))
			{
				var openBraceIndent = new Indent(braces.Open);
				var closeBraceIndent = new Indent(braces.Close);
				if (openBraceIndent.IndentedTokenIsFirstAtLine && !closeBraceIndent.IndentedTokenIsFirstAtLine)
					yield return new SolutionStyleError(StyleErrorType.Indents05, braces.Close);
			}
		}
	}
}