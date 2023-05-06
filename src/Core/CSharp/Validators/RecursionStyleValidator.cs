using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ulearn.Core.CSharp.Validators
{
	public class RecursionStyleValidator : ICSharpSolutionValidator, IStrictValidator
	{
		private readonly bool requireRecursion;

		public RecursionStyleValidator(bool requireRecursion)
		{
			this.requireRecursion = requireRecursion;
		}

		public List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel)
		{
			var recursiveMethods = userSolution.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(IsRecursive).ToList();

			SolutionStyleError error = null;
			if (requireRecursion && !recursiveMethods.Any())
				error = new SolutionStyleError(StyleErrorType.Recursion01, userSolution.GetRoot());

			if (!requireRecursion && recursiveMethods.Any())
				error = new SolutionStyleError(StyleErrorType.Recursion02, recursiveMethods.First());

			if (error != null)
				return new List<SolutionStyleError> { error };

			return new List<SolutionStyleError>();
		}

		private static bool IsRecursive(MethodDeclarationSyntax method)
		{
			return method.Body != null && method.Body.DescendantNodes()
				.OfType<SimpleNameSyntax>()
				.Any(n => n.Identifier.ValueText == method.Identifier.ValueText);
		}
	}
}