using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Ulearn.Core.CSharp.Validators.IndentsValidation.Reporters
{
	internal static class CompilationUnitChildrenNotConsistentReporter
	{
		public static IEnumerable<SolutionStyleError> Report(SyntaxTree userSolution)
		{
			var childLineIndents = userSolution.GetRoot().ChildNodes()
				.Select(node => node.GetFirstToken())
				.Select(t => new Indent(t))
				.Where(i => i.IndentedTokenIsFirstAtLine)
				.ToList();
			if (!childLineIndents.Any())
				return Enumerable.Empty<SolutionStyleError>();

			var firstIndent = childLineIndents.First();
			return childLineIndents
				.Skip(1)
				.Where(i => i.LengthInSpaces != firstIndent.LengthInSpaces)
				.Select(i => new SolutionStyleError(StyleErrorType.Indents06, i.IndentedToken));
		}
	}
}