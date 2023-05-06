using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ulearn.Core.CSharp.Validators
{
	public class IsStaticMethodValidator : ICSharpSolutionValidator, IStrictValidator
	{
		public List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel)
		{
			var error = FindSingleError(userSolution);
			if (error == null)
				return new List<SolutionStyleError>();

			return new List<SolutionStyleError> { error };
		}

		private SolutionStyleError FindSingleError(SyntaxTree userSolution)
		{
			if (userSolution.GetRoot() is not CompilationUnitSyntax cu)
				return new SolutionStyleError(StyleErrorType.Static01, userSolution.GetRoot());
			if (cu.Members.Count > 1)
				return new SolutionStyleError(StyleErrorType.Static02, cu.Members[1]);
			var method = cu.Members[0] as MethodDeclarationSyntax; // null, потому что метод вне класса считается локальной функцией
			var localFunction = (cu.Members[0] as GlobalStatementSyntax)?.Statement as LocalFunctionStatementSyntax;
			if (method == null && localFunction == null)
				return new SolutionStyleError(StyleErrorType.Static01, cu.Members[0]);
			if (method != null)
				return FindErrorInMethodDeclaration(method);
			return FindErrorInLocalFunctionDeclaration(localFunction);
		}

		protected virtual SolutionStyleError FindErrorInMethodDeclaration(MethodDeclarationSyntax method)
		{
			return null;
		}

		protected virtual SolutionStyleError FindErrorInLocalFunctionDeclaration(LocalFunctionStatementSyntax method)
		{
			return null;
		}
	}
}