using System;
using System.Linq;
using AntiPlagiarism.Api.Models;
using AntiPlagiarism.Web.CodeAnalyzing;
using AntiPlagiarism.Web.Extensions;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace AntiPlagiarism.Tests.CodeAnalyzing;

[TestFixture]
public class SnippetsExtractor_should
{
	[Test]
	public void ExtractSnippets()
	{
		const int snippetTokensCount = 12;

		var syntaxTree = CSharpSyntaxTree.ParseText(CommonTestData.SimpleProgramWithMethodAndProperty);
		var syntaxTreeRoot = syntaxTree.GetRoot();
		var tokens = syntaxTreeRoot.GetTokens().Select(t => new CSharpToken(t)).ToList();

		var extractor = new SnippetsExtractor();
		var converter = new TokensKindsConverter();
		var snippets = extractor.GetSnippets(tokens, snippetTokensCount, converter).ToList();

		Assert.AreEqual(tokens.Count - snippetTokensCount + 1, snippets.Count);
		foreach (var snippet in snippets)
		{
			Assert.AreEqual(SnippetType.TokensKinds, snippet.SnippetType);
			Assert.AreEqual(snippetTokensCount, snippet.TokensCount);
			Console.WriteLine(snippet);
		}

		var lastTokens = tokens.TakeLast(snippetTokensCount).ToList();
		var hasher = SnippetsExtractorOptions.Default.SequenceHasher;
		foreach (var token in lastTokens)
			hasher.Enqueue(converter.Convert(token));

		Assert.AreEqual(hasher.GetCurrentHash(), snippets.Last().Hash);
	}
}