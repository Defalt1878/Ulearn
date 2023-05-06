using System.Collections.Generic;
using System.Linq;

namespace Ulearn.Core.CSharp.Validators.IndentsValidation.Reporters
{
	internal static class BracesContentNotIndentedOrNotConsistentReporter
	{
		public static IEnumerable<SolutionStyleError> Report(IEnumerable<BracesPair> bracesPairs)
		{
			foreach (var braces in bracesPairs.Where(pair => pair.Open.GetLine() != pair.Close.GetLine()))
			{
				var childLineIndents = braces.Open.Parent!.ChildNodes()
					.Select(node => node.DescendantTokens().First())
					.Where(t => braces.TokenInsideBraces(t))
					.Select(t => new Indent(t))
					.Where(i => i.IndentedTokenIsFirstAtLine)
					.ToList();
				if (!childLineIndents.Any())
					continue;
				var firstTokenOfLineWithMinimalIndent = Indent.TokenIsFirstAtLine(braces.Open)
					? braces.Open
					: braces.Open.GetFirstTokenOfCorrectOpenBraceParent();
				if (firstTokenOfLineWithMinimalIndent == default)
					continue;
				var minimalIndentAfterOpenBrace = new Indent(firstTokenOfLineWithMinimalIndent);
				var firstChild = childLineIndents.First();
				if (firstChild.LengthInSpaces <= minimalIndentAfterOpenBrace.LengthInSpaces)
					yield return new SolutionStyleError(StyleErrorType.Indents01, firstChild.IndentedToken, braces);
				var badLines = childLineIndents.Where(t => t.LengthInSpaces != firstChild.LengthInSpaces);
				foreach (var badIndent in badLines)
					yield return new SolutionStyleError(StyleErrorType.Indents02, badIndent.IndentedToken, braces);
			}
		}
	}
}