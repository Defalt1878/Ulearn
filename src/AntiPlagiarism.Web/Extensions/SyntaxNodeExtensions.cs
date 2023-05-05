using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AntiPlagiarism.Web.Extensions;

public static class SyntaxNodeExtensions
{
	public static IEnumerable<SyntaxToken> GetTokens(this SyntaxNode node)
	{
		var current = node.GetFirstToken();
		var last = node.GetLastToken();
		while (current != last)
		{
			yield return current;
			current = current.GetNextToken();
		}

		yield return current;
	}
}