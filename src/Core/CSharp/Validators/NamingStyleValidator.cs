using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ulearn.Core.CSharp.Validators
{
	public class NamingStyleValidator : BaseStyleValidator
	{
		public override List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel)
		{
			var methodErrors = InspectAll<MethodDeclarationSyntax>(userSolution, InspectMethod);
			var localFunctionErrors = InspectAll<LocalFunctionStatementSyntax>(userSolution, InspectMethod);
			return methodErrors.Concat(localFunctionErrors).ToList();
		}

		private static IEnumerable<SolutionStyleError> InspectMethod(LocalFunctionStatementSyntax method)
		{
			var name = method?.Identifier.Text;
			if (name == null || method.AttributeLists.Any())
				yield break;
			if (method.AttributeLists.Any())
				yield break; // Turn this check off for [Test], [TestCase] and all other special cases marked with attribute
			if (method.IsVoidGetter() && !method.AttributeLists.Any())
				yield return new SolutionStyleError(StyleErrorType.NamingStyle01, method.Identifier);
			if (method.IsNoArgsSetter())
				yield return new SolutionStyleError(StyleErrorType.NamingStyle02, method.Identifier);
			if (name.IsSingleWordGerundIdentifier())
				yield return new SolutionStyleError(StyleErrorType.NamingStyle03, method.Identifier);
			if (name.IsSingleWordIonIdentifier())
				yield return new SolutionStyleError(StyleErrorType.NamingStyle04, method.Identifier);
		}

		private static IEnumerable<SolutionStyleError> InspectMethod(MethodDeclarationSyntax method)
		{
			var name = method?.Identifier.Text;
			if (name == null || method.AttributeLists.Any())
				yield break;
			if (method.AttributeLists.Any())
				yield break; // Turn this check off for [Test], [TestCase] and all other special cases marked with attribute
			if (method.IsVoidGetter() && !method.AttributeLists.Any())
				yield return new SolutionStyleError(StyleErrorType.NamingStyle01, method.Identifier);
			if (method.IsNoArgsSetter())
				yield return new SolutionStyleError(StyleErrorType.NamingStyle02, method.Identifier);
			if (name.IsSingleWordGerundIdentifier())
				yield return new SolutionStyleError(StyleErrorType.NamingStyle03, method.Identifier);
			if (name.IsSingleWordIonIdentifier())
				yield return new SolutionStyleError(StyleErrorType.NamingStyle04, method.Identifier);
		}
	}
}