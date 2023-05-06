using System.Collections.Generic;
using System.Linq;

namespace Ulearn.Core.CSharp.Validators.IndentsValidation.Reporters
{
	internal static class OpenBraceHasCodeOnSameLineReporter
	{
		public static IEnumerable<SolutionStyleError> Report(IEnumerable<BracesPair> bracesPairs)
		{
			foreach (var braces in bracesPairs.Where(pair => pair.Open.GetLine() != pair.Close.GetLine()))
			{
				var openBraceHasCodeOnSameLine = braces.Open.Parent!.ChildNodes()
					.Select(node => node.GetFirstToken())
					.Any(t => braces.TokenInsideBraces(t) && t.GetLine() == braces.Open.GetLine());
				if (openBraceHasCodeOnSameLine)
					yield return new SolutionStyleError(StyleErrorType.Indents12, braces.Open);
			}
		}
	}
}