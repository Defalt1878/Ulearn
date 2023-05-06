using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ulearn.Core.CSharp.Validators
{
	public class SingleStatementMethodValidator : IsStaticMethodValidator
	{
		protected override SolutionStyleError FindErrorInMethodDeclaration(MethodDeclarationSyntax method)
		{
			return FindErrorInMethodBody(method.Body);
		}

		protected override SolutionStyleError FindErrorInLocalFunctionDeclaration(LocalFunctionStatementSyntax method)
		{
			return FindErrorInMethodBody(method.Body);
		}

		private static SolutionStyleError FindErrorInMethodBody(BlockSyntax body)
		{
			var hasError = body.Statements.Count != 1 || body.Statements.Single() is not ReturnStatementSyntax;
			if (hasError)
				return new SolutionStyleError(StyleErrorType.Single01, body);
			return null;
		}
	}
}