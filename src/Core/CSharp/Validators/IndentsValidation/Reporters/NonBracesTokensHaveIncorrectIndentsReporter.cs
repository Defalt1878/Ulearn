using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ulearn.Core.CSharp.Validators.IndentsValidation.Reporters
{
	internal static class NonBracesTokensHaveIncorrectIndentsReporter
	{
		public static IEnumerable<SolutionStyleError> Report(SyntaxTree tree)
		{
			return tree.GetRoot().DescendantNodes()
				.Where(NeedToValidateNonBracesTokens)
				.Select(x => SyntaxNodeOrToken.Create(tree, x, true))
				.SelectMany(CheckNonBracesStatements)
				.Distinct();
		}

		private static IEnumerable<SolutionStyleError> CheckNonBracesStatements(SyntaxNodeOrToken rootStatementSyntax)
		{
			var rootLine = rootStatementSyntax.GetStartLine();
			var rootEndLine = rootStatementSyntax.GetConditionEndLine();
			var parent = rootStatementSyntax.GetParent();
			var rootStart = rootStatementSyntax.GetValidationStartIndexInSpaces();

			if (parent is { Kind: SyntaxKind.Block })
			{
				var parentLine = parent.GetStartLine();
				if (parentLine == rootLine)
					yield break;
				var parentStart = parent.GetValidationStartIndexInSpaces();
				if (rootStart == 0)
					yield break;
				var errorType = GetIndentErrorType(rootStart, parentStart, rootLine, parentLine);
				if (errorType.HasValue)
				{
					yield return new SolutionStyleError(errorType.Value, rootStatementSyntax);
					yield break;
				}
			}

			var statementClauses = rootStatementSyntax.SyntaxNode.CreateStatements().ToArray();
			var reports = ValidateStatementClauses(statementClauses, rootStatementSyntax, rootStart, rootLine, rootEndLine);
			foreach (var report in reports)
				yield return report;
		}

		private static IEnumerable<SolutionStyleError> ValidateStatementClauses(
			IEnumerable<SyntaxNodeOrToken> statementClauses,
			SyntaxNodeOrToken rootStatementSyntax,
			int rootStart,
			int rootLine,
			int rootEndLine)
		{
			foreach (var statementClause in statementClauses)
			{
				if (statementClause.Kind == SyntaxKind.Block)
					break;
				var statementStart = statementClause.GetValidationStartIndexInSpaces();
				var statementLine = statementClause.GetStartLine();

				if (statementClause.HasExcessNewLines())
				{
					yield return new SolutionStyleError(StyleErrorType.Indents10, statementClause);
					continue;
				}

				if (!statementClause.OnSameIndentWithParent.HasValue)
				{
					if (statementStart != rootStart)
					{
						var errorType = GetIndentErrorType(statementStart, rootStart, statementLine, rootLine);
						if (errorType.HasValue)
						{
							yield return new SolutionStyleError(errorType.Value, statementClause);
							continue;
						}
					}
				}
				else
				{
					if (statementClause.OnSameIndentWithParent.Value)
					{
						if (statementStart != rootStart)
						{
							yield return new SolutionStyleError(StyleErrorType.Indents11, statementClause);
							continue;
						}
					}
					else
					{
						if (IsAllowedOneLineSyntaxToken(rootEndLine, statementLine, rootStatementSyntax, statementClause))
							continue;
						var errorType = GetIndentErrorType(statementStart, rootStart, statementLine, rootLine);
						if (errorType.HasValue)
						{
							yield return new SolutionStyleError(errorType.Value, statementClause);
							continue;
						}
					}
				}

				foreach (var nestedError in CheckNonBracesStatements(statementClause))
					yield return nestedError;
			}
		}

		private static bool IsAllowedOneLineSyntaxToken(
			int rootEndLine,
			int statementLine,
			SyntaxNodeOrToken rootStatementSyntax,
			SyntaxNodeOrToken statementClause)
		{
			return rootEndLine == statementLine &&
					rootStatementSyntax.Kind is
						SyntaxKind.IfStatement or
						SyntaxKind.ElseClause &&
					statementClause.Kind is not
						SyntaxKind.IfStatement and not
						SyntaxKind.ForStatement and not
						SyntaxKind.ForEachStatement and not
						SyntaxKind.WhileStatement and not
						SyntaxKind.DoStatement;
		}

		private static StyleErrorType? GetIndentErrorType(int statementStart,
			int rootStart,
			int statementLine,
			int rootLine)
		{
			if (statementLine == rootLine)
				return StyleErrorType.Indents07;

			if (statementStart <= rootStart)
				return StyleErrorType.Indents08;

			var delta = statementStart - rootStart;
			if (delta < 4)
				return StyleErrorType.Indents09;

			return null;
		}

		private static bool NeedToValidateNonBracesTokens(SyntaxNode syntaxNode)
		{
			var syntaxKind = syntaxNode.Kind();
			return syntaxKind is
				SyntaxKind.IfStatement or
				SyntaxKind.WhileStatement or
				SyntaxKind.ForStatement or
				SyntaxKind.ForEachStatement or
				SyntaxKind.DoStatement or
				SyntaxKind.UsingStatement;
		}
	}
}