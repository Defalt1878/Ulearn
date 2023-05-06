using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ulearn.Core.CSharp.Validators.IndentsValidation
{
	internal class Indent
	{
		public int LengthInSpaces { get; }
		public bool IndentedTokenIsFirstAtLine { get; }
		public SyntaxToken IndentedToken { get; }

		public Indent(SyntaxToken indentedToken)
		{
			IndentedToken = indentedToken;
			IndentedTokenIsFirstAtLine = TokenIsFirstAtLine(indentedToken);
			var firstTokenInLine = GetFirstTokenAtLine(indentedToken);
			LengthInSpaces = GetLengthInSpaces(firstTokenInLine);
		}

		private static int GetLengthInSpaces(SyntaxToken firstTokenInLine)
		{
			var lastTrivia = firstTokenInLine.LeadingTrivia.LastOrDefault();
			if (!lastTrivia.IsKind(SyntaxKind.WhitespaceTrivia))
				return 0;
			var stringTrivia = lastTrivia.ToFullString();
			var type = GetIndentType(stringTrivia);
			if (type == IndentType.Whitespace)
				return stringTrivia.Length;
			if (type == IndentType.Tab)
				return stringTrivia.Length * 4;
			return stringTrivia.Count(c => c == ' ') + stringTrivia.Count(c => c == '\t') * 4;
		}

		public static IndentType GetIndentType(string leadingTrivia)
		{
			if (leadingTrivia.All(v => v == '\t'))
				return IndentType.Tab;
			if (leadingTrivia.All(v => v == ' '))
				return IndentType.Whitespace;
			return IndentType.Mixed;
		}

		public static SyntaxToken GetFirstTokenAtLine(SyntaxToken token)
		{
			var tokenLineSpanStart = token.SyntaxTree!.GetText().Lines[token.GetLine()].Start;
			return token.SyntaxTree.GetRoot().FindToken(tokenLineSpanStart);
		}

		public static bool TokenIsFirstAtLine(SyntaxToken token)
		{
			return GetFirstTokenAtLine(token).IsEquivalentTo(token);
		}

		public override string ToString()
		{
			return $"{IndentedToken.Kind()}. LengthInSpaces: {LengthInSpaces}";
		}
	}
}