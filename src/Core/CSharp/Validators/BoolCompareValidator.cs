using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ulearn.Core.CSharp.Validators
{
	public class BoolCompareValidator : BaseStyleValidator
	{
		public override List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel)
		{
			return InspectAll<BinaryExpressionSyntax>(userSolution, semanticModel, Inspect).ToList();
		}

		private static IEnumerable<SolutionStyleError> Inspect(BinaryExpressionSyntax binaryExpression, SemanticModel semanticModel)
		{
			var operatorKind = binaryExpression.OperatorToken.Kind();
			if (operatorKind != SyntaxKind.EqualsEqualsToken && operatorKind != SyntaxKind.ExclamationEqualsToken)
				yield break;
			var leftNodeTypeInfo = semanticModel.GetTypeInfo(binaryExpression.Left);
			var leftNodeTree = binaryExpression.Left as LiteralExpressionSyntax;
			var rightNodeTypeInfo = semanticModel.GetTypeInfo(binaryExpression.Right);
			var rightNodeTree = binaryExpression.Right as LiteralExpressionSyntax;
			if (IsBooleanType(rightNodeTypeInfo) && IsBooleanType(leftNodeTypeInfo)
												&& (IsBoolLiteral(leftNodeTree) || IsBoolLiteral(rightNodeTree)))
				yield return new SolutionStyleError(StyleErrorType.BoolCompare01, binaryExpression);
		}

		private static bool IsBoolLiteral(LiteralExpressionSyntax node) =>
			node?.Token.Kind() is SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword;

		private static bool IsBooleanType(TypeInfo node) => node.Type is { Name: "Boolean" };
	}
}