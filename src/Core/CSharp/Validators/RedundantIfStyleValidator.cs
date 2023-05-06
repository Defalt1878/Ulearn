using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ulearn.Common.Extensions;

namespace Ulearn.Core.CSharp.Validators
{
	public class RedundantIfStyleValidator : BaseStyleValidator
	{
		public override List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel)
		{
			return InspectAll<IfStatementSyntax>(userSolution, Inspect).ToList();
		}

		private static IEnumerable<SolutionStyleError> Inspect(IfStatementSyntax ifElseStatement)
		{
			var trueStatementIsReturnBoolLiteral =
				(ifElseStatement.Statement as ReturnStatementSyntax)
				.Call(r => r.Expression as LiteralExpressionSyntax)
				.Call(IsBoolLiteral, false);
			var falseStatementIsReturnBoolLiteral =
				ifElseStatement.Else
					.Call(e => e.Statement as ReturnStatementSyntax)
					.Call(r => r.Expression as LiteralExpressionSyntax)
					.Call(e => (bool?)IsBoolLiteral(e), null);

			var nextSibling = ifElseStatement.Parent!.ChildNodes().SkipWhile(n => n != ifElseStatement).Skip(1).FirstOrDefault();
			falseStatementIsReturnBoolLiteral ??= (nextSibling as ReturnStatementSyntax)
				.Call(r => r.Expression as LiteralExpressionSyntax)
				.Call(IsBoolLiteral, false);
			if (trueStatementIsReturnBoolLiteral && falseStatementIsReturnBoolLiteral == true)
				yield return new SolutionStyleError(StyleErrorType.RedundantIf01, ifElseStatement);
		}

		private static bool IsBoolLiteral(LiteralExpressionSyntax node)
		{
			var token = node.Token.Kind();
			return token is SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword;
		}
	}
}