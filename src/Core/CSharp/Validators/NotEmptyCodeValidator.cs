using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ulearn.Core.CSharp.Validators
{
	public class NotEmptyCodeValidator : BaseStyleValidator, IStrictValidator
	{
		public override List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel)
		{
			var hasCode = userSolution.GetRoot().DescendantNodes().Any(n => n is StatementSyntax or MemberDeclarationSyntax);
			if (!hasCode)
				return new List<SolutionStyleError>
				{
					new(StyleErrorType.NotEmpty01, userSolution.GetRoot())
				};

			return new List<SolutionStyleError>();
		}
	}
}