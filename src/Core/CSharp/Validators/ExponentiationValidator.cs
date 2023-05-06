using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ulearn.Core.CSharp.Validators
{
	public class ExponentiationValidator : BaseStyleValidator
	{
		private static readonly HashSet<double> forbiddenDegrees = new() { 2.0, 3.0 };

		public override List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel)
		{
			return InspectAll<InvocationExpressionSyntax>(userSolution, semanticModel, InspectMethod).ToList();
		}

		private static IEnumerable<SolutionStyleError> InspectMethod(InvocationExpressionSyntax methodInvocation, SemanticModel semanticModel)
		{
			var symbol = semanticModel.GetSymbolInfo(methodInvocation).Symbol;
			if (methodInvocation.IsMathPow(symbol) &&
				methodInvocation.ArgumentList.ContainsForbiddenDegrees(forbiddenDegrees))
				yield return new SolutionStyleError(StyleErrorType.Exponentiation01, methodInvocation);
		}
	}
}