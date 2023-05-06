using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ulearn.Common.Extensions;
using Ulearn.Core.Properties;

namespace Ulearn.Core.CSharp.Validators.VerbInMethodNameValidation
{
	public class VerbInMethodNameValidator : BaseStyleValidator
	{
		private readonly HashSet<string> englishVerbs;

		private readonly HashSet<string> exceptionsMethodNames;
		private readonly HashSet<string> exceptionsPreposition;

		public VerbInMethodNameValidator(string[] exceptions = null)
		{
			exceptions ??= Array.Empty<string>();
			exceptionsMethodNames = new HashSet<string>(exceptions.Concat(new[] { "Main" }), StringComparer.InvariantCultureIgnoreCase);
			exceptionsPreposition = new HashSet<string>(new[] { "For", "To", "With", "From", "At" }, StringComparer.InvariantCultureIgnoreCase);
			englishVerbs = new HashSet<string>(Resources.englishVerbs.SplitToLines(), StringComparer.InvariantCultureIgnoreCase);
		}

		public override List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel)
		{
			return InspectAll<MethodDeclarationSyntax>(userSolution, s => InspectNames(s.Identifier()))
				.Concat(InspectAll<LocalFunctionStatementSyntax>(userSolution, s => InspectNames(s.Identifier))).ToList();
		}

		private IEnumerable<SolutionStyleError> InspectNames(SyntaxToken syntaxToken)
		{
			if (exceptionsMethodNames.Contains(syntaxToken.ValueText))
				yield break;

			var wordsInName = syntaxToken.ValueText.SplitByCamelCase().ToList();

			if (wordsInName.Count == 0)
				yield break;

			if (exceptionsPreposition.Contains(wordsInName.First()))
				yield break;

			if (wordsInName.Any(word => englishVerbs.Contains(word)))
				yield break;

			yield return new SolutionStyleError(StyleErrorType.VerbInMethod01, syntaxToken);
		}
	}
}