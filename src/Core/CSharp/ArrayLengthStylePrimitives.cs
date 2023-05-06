using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ulearn.Core.CSharp
{
	public static class ArrayLengthStylePrimitives
	{
		public static bool IsCycle(this SyntaxNode syntaxNode)
		{
			return syntaxNode is
				ForStatementSyntax or
				WhileStatementSyntax or
				DoStatementSyntax or
				ForEachStatementSyntax;
		}

		public static bool ContainsAssignmentOf(this StatementSyntax statementSyntax, string variableName,
			SemanticModel semanticModel)
		{
			if (!statementSyntax.IsCycle())
				return false;

			var assignments = statementSyntax
				.DescendantNodes()
				.OfType<AssignmentExpressionSyntax>()
				.ToList();
			var declarations = statementSyntax
				.DescendantNodes()
				.OfType<VariableDeclaratorSyntax>()
				.ToList();

			foreach (var assignment in assignments)
			{
				var variable = assignment.Left;
				if (variable.HasName(variableName, semanticModel))
					return true;
			}

			foreach (var declaration in declarations)
			{
				var variable = declaration.Identifier;
				if (variable.Text == variableName)
					return true;
			}

			return false;
		}

		public static bool HasName(this SyntaxNode node, string variableName, SemanticModel semanticModel)
		{
			var symbol = semanticModel.GetSymbolInfo(node).Symbol;
			return symbol?.ToString() == variableName;
		}

		public static bool HasVariableAsArgument(this InvocationExpressionSyntax methodInvocation, string variableName, SemanticModel semanticModel)
		{
			var arguments = methodInvocation.ArgumentList.Arguments;
			return arguments
				.Select(v => semanticModel.GetSymbolInfo(v).Symbol)
				.Any(s => s?.ToString() == variableName);
		}
	}
}