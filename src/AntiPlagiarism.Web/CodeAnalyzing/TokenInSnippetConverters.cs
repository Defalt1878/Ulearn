using AntiPlagiarism.Api.Models;

namespace AntiPlagiarism.Web.CodeAnalyzing;

public interface ITokenInSnippetConverter
{
	string Convert(IToken token);
	SnippetType SnippetType { get; }
}

public class TokensKindsConverter : ITokenInSnippetConverter
{
	public string Convert(IToken token)
	{
		return token.Type;
	}

	public SnippetType SnippetType => SnippetType.TokensKinds;
}

public class TokensValuesConverter : ITokenInSnippetConverter
{
	public string Convert(IToken token)
	{
		return token.Value;
	}

	public SnippetType SnippetType => SnippetType.TokensValues;
}