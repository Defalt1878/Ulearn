using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ulearn.Core.CSharp.Validators
{
	public class BracketValidator : BaseStyleValidator
	{
		public override List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel)
		{
			return InspectAll<ReturnStatementSyntax>(userSolution, InspectReturnStatement).ToList();
		}

		private IEnumerable<SolutionStyleError> InspectReturnStatement(ReturnStatementSyntax returnStatementSyntax)
		{
			var expression = returnStatementSyntax.Expression;
			if (expression == null)
				yield break;

			if (expression is TupleExpressionSyntax)
				yield break;

			var syntaxKinds = expression.DescendantTokens().Select(x => x.Kind()).ToArray();
			if (!NeedToCheckExpression(syntaxKinds))
				yield break;

			if (ContainsRedundantBrackets(syntaxKinds))
				yield return new SolutionStyleError(StyleErrorType.Brackets01, returnStatementSyntax);
		}

		private static bool NeedToCheckExpression(IReadOnlyList<SyntaxKind> syntaxKinds)
		{
			return syntaxKinds.Count > 2 &&
					syntaxKinds[0] == SyntaxKind.OpenParenToken &&
					syntaxKinds[^1] == SyntaxKind.CloseParenToken;
		}

		private static bool ContainsRedundantBrackets(IEnumerable<SyntaxKind> syntaxKinds)
		{
			var pairBracketsCount = 0;
			var brackets = syntaxKinds
				.Where(x => x is SyntaxKind.OpenParenToken or SyntaxKind.CloseParenToken)
				.ToArray();

			for (var i = 1; i < brackets.Length - 1; i++)
			{
				if (brackets[i] == SyntaxKind.OpenParenToken)
					pairBracketsCount++;
				else
					pairBracketsCount--;

				if (pairBracketsCount < 0)
					return false;
			}

			return pairBracketsCount == 0;
		}
	}
}