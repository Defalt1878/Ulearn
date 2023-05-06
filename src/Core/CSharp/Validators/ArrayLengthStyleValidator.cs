using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ulearn.Core.CSharp.Validators
{
	public class ArrayLengthStyleValidator : BaseStyleValidator
	{
		public override List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel)
		{
			var errorsInForCycle = InspectAll<ForStatementSyntax>(userSolution, semanticModel, InspectMethod);
			var errorsInForEachCycle = InspectAll<ForEachStatementSyntax>(userSolution, semanticModel, InspectMethod);
			var errorsInWhileCycle = InspectAll<WhileStatementSyntax>(userSolution, semanticModel, InspectMethod);
			var errorsInDoWhileCycle = InspectAll<DoStatementSyntax>(userSolution, semanticModel, InspectMethod);

			return errorsInForCycle
				.Concat(errorsInForEachCycle)
				.Concat(errorsInWhileCycle)
				.Concat(errorsInDoWhileCycle)
				.ToList();
		}

		private static IEnumerable<SolutionStyleError> InspectMethod<TCycle>(TCycle cycleStatement, SemanticModel semanticModel) where TCycle : StatementSyntax
		{
			var methodInvocations = (cycleStatement is ForStatementSyntax forStatement
					? (forStatement.Condition?.DescendantNodes()).EmptyIfNull().Concat(forStatement.Statement.DescendantNodes())
					: cycleStatement.DescendantNodes())
				.OfType<InvocationExpressionSyntax>()
				.ToList();

			foreach (var methodInvocation in methodInvocations)
			{
				var symbol = ModelExtensions.GetSymbolInfo(semanticModel, methodInvocation).Symbol;
				var argumentExpression = methodInvocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
				if (symbol?.ToString() != "System.Array.GetLength(int)"
					|| argumentExpression == null
					|| !argumentExpression.IsKind(SyntaxKind.NumericLiteralExpression))
					continue;

				var variable = methodInvocation
					.Expression
					.DescendantNodes()
					.FirstOrDefault();

				if (variable != null)
				{
					var variableSymbol = semanticModel.GetSymbolInfo(variable).Symbol;
					var variableName = variableSymbol?.ToString();
					if (!cycleStatement.ContainsAssignmentOf(variableName, semanticModel)
						&& !methodInvocations.Any(m => m.HasVariableAsArgument(variableName, semanticModel)))
						yield return new SolutionStyleError(StyleErrorType.ArrayLength01, methodInvocation);
				}
			}
		}
	}
}