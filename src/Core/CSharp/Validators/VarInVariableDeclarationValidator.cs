using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ulearn.Core.Extensions;

namespace Ulearn.Core.CSharp.Validators
{
	public class VarInVariableDeclarationValidator : BaseStyleValidator
	{
		public override List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel)
		{
			return InspectAll<VariableDeclarationSyntax>(userSolution, semanticModel, Inspect).ToList();
		}

		private static IEnumerable<SolutionStyleError> Inspect(VariableDeclarationSyntax variableDeclarationSyntax, SemanticModel semanticModel)
		{
			if (!NeedToCheckDeclaration(variableDeclarationSyntax))
				yield break;

			foreach (var variable in variableDeclarationSyntax.Variables)
			{
				if (variable.Initializer == null)
					yield break;

				var value = variable.Initializer.Value;
				if (!(value is LiteralExpressionSyntax) && !(value is ObjectCreationExpressionSyntax))
					continue;

				var initializerTypeInfo = semanticModel.GetTypeInfo(variable.Initializer.Value);
				var variableTypeInfo = semanticModel.GetTypeInfo(variableDeclarationSyntax.Type);

				if (variableTypeInfo.IsPrimitive())
					yield break;

				if (SymbolEqualityComparer.Default.Equals(initializerTypeInfo.Type, variableTypeInfo.Type))
					yield return new SolutionStyleError(StyleErrorType.VarInVariableDeclaration01, variable);
			}
		}

		private static bool NeedToCheckDeclaration(VariableDeclarationSyntax variableDeclarationSyntax)
		{
			if (variableDeclarationSyntax.Type.IsVar)
				return false;

			var parent = variableDeclarationSyntax.Parent;
			if (parent is LocalDeclarationStatementSyntax localDeclarationStatement)
				return !localDeclarationStatement.IsConst;

			return variableDeclarationSyntax.Parent is ForStatementSyntax;
		}
	}
}