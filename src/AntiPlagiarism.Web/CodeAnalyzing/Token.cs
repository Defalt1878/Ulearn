using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AntiPlagiarism.Web.CodeAnalyzing;

public interface IToken
{
	public string Type { get; set; }
	public string Value { get; set; }
	public int Position { get; set; }
}

public class Token : IToken
{
	public string Type { get; set; }
	public string Value { get; set; }
	public int Position { get; set; }

	public override string ToString()
	{
		return $"{Type} {Value}";
	}
}

public class CSharpToken : IToken
{
	public string Type { get; set; }
	public string Value { get; set; }
	public int Position { get; set; }

	public CSharpToken(SyntaxToken syntaxToken)
	{
		Type = syntaxToken.Kind().ToString();
		Value = syntaxToken.ToString();
		Position = syntaxToken.SpanStart;
	}

	public override string ToString()
	{
		return $"{Type} {Value}";
	}
}