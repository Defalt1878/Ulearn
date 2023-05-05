using System.Collections.Generic;
using System.Linq;

namespace AntiPlagiarism.Web.CodeAnalyzing;

public class CodeUnit
{
	public CodePath Path { get; }
	public List<IToken> Tokens { get; }
	public int FirstTokenIndex { get; set; }

	public int Position => Tokens.FirstOrDefault()?.Position ?? 0;

	public CodeUnit(CodePath path, IEnumerable<IToken> tokens, int firstTokenIndex = 0)
	{
		Path = path;
		Tokens = tokens.ToList();
		FirstTokenIndex = firstTokenIndex;
	}

	public override string ToString()
	{
		var tokensString = string.Join(" ", Tokens.Select(t => t.ToString()));
		return $"CodeUnit({tokensString} at {Path}, position {Position})";
	}
}