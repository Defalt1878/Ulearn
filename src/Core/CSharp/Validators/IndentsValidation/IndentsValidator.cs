using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Ulearn.Core.CSharp.Validators.IndentsValidation.Reporters;

namespace Ulearn.Core.CSharp.Validators.IndentsValidation
{
	public class IndentsValidator : BaseStyleValidator
	{
		private BracesPair[] bracesPairs;
		private SyntaxTree tree;

		public override List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel)
		{
			tree = userSolution;
			bracesPairs = BuildBracesPairs().OrderBy(p => p.Open.SpanStart).ToArray();

			var errors = CompilationUnitChildrenNotConsistentReporter.Report(tree)
				.Concat(BracesNotAlignedReporter.Report(bracesPairs))
				.Concat(CloseBraceHasCodeOnSameLineReporter.Report(bracesPairs))
				.Concat(OpenBraceHasCodeOnSameLineReporter.Report(bracesPairs))
				.Concat(BracesContentNotIndentedOrNotConsistentReporter.Report(bracesPairs))
				.Concat(BracesNotIndentedReporter.Report(bracesPairs))
				.Concat(NonBracesTokensHaveIncorrectIndentsReporter.Report(userSolution))
				.ToList();
			return errors;
		}

		private IEnumerable<BracesPair> BuildBracesPairs()
		{
			var braces = tree.GetRoot().DescendantTokens()
				.Where(t => t.IsKind(SyntaxKind.OpenBraceToken) || t.IsKind(SyntaxKind.CloseBraceToken));
			var openBracesStack = new Stack<SyntaxToken>();
			foreach (var brace in braces)
				if (brace.IsKind(SyntaxKind.OpenBraceToken))
					openBracesStack.Push(brace);
				else
					yield return new BracesPair(openBracesStack.Pop(), brace);
		}
	}
}