using System;
using System.Collections.Generic;
using System.Linq;
using AntiPlagiarism.Web.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace AntiPlagiarism.Tests.Extensions;

[TestFixture]
public class SyntaxNodeExtensions_should
{
	[Test]
	public void TestGetTokens()
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(CommonTestData.SimpleProgramWithMethodAndProperty);
		var syntaxTreeRoot = syntaxTree.GetRoot();
		var tokens = syntaxTreeRoot.GetTokens().ToList();

		CollectionAssert.AllItemsAreInstancesOfType(tokens, typeof(SyntaxToken));
		var kinds = new List<SyntaxKind>
		{
			SyntaxKind.UsingKeyword,
			SyntaxKind.IdentifierToken,
			SyntaxKind.SemicolonToken,
			SyntaxKind.UsingKeyword,
			SyntaxKind.IdentifierToken,
			SyntaxKind.DotToken,
			SyntaxKind.IdentifierToken,
			SyntaxKind.SemicolonToken,
			SyntaxKind.UsingKeyword,
			SyntaxKind.IdentifierToken,
			SyntaxKind.DotToken,
			SyntaxKind.IdentifierToken,
			SyntaxKind.SemicolonToken,
			SyntaxKind.UsingKeyword,
			SyntaxKind.IdentifierToken,
			SyntaxKind.DotToken,
			SyntaxKind.IdentifierToken,
			SyntaxKind.SemicolonToken,
			SyntaxKind.NamespaceKeyword,
			SyntaxKind.IdentifierToken,
			SyntaxKind.DotToken,
			SyntaxKind.IdentifierToken,
			SyntaxKind.OpenBraceToken,
			SyntaxKind.ClassKeyword,
			SyntaxKind.IdentifierToken,
			SyntaxKind.OpenBraceToken,
			SyntaxKind.StaticKeyword,
			SyntaxKind.VoidKeyword,
			SyntaxKind.IdentifierToken,
			SyntaxKind.OpenParenToken,
			SyntaxKind.CloseParenToken,
			SyntaxKind.OpenBraceToken,
			SyntaxKind.IdentifierToken,
			SyntaxKind.DotToken,
			SyntaxKind.IdentifierToken,
			SyntaxKind.OpenParenToken,
			SyntaxKind.StringLiteralToken,
			SyntaxKind.CloseParenToken,
			SyntaxKind.SemicolonToken,
			SyntaxKind.CloseBraceToken,
			SyntaxKind.StaticKeyword,
			SyntaxKind.IntKeyword,
			SyntaxKind.IdentifierToken,
			SyntaxKind.OpenBraceToken,
			SyntaxKind.GetKeyword,
			SyntaxKind.OpenBraceToken,
			SyntaxKind.ReturnKeyword,
			SyntaxKind.NumericLiteralToken,
			SyntaxKind.SemicolonToken,
			SyntaxKind.CloseBraceToken,
			SyntaxKind.SetKeyword,
			SyntaxKind.OpenBraceToken,
			SyntaxKind.IdentifierToken,
			SyntaxKind.DotToken,
			SyntaxKind.IdentifierToken,
			SyntaxKind.OpenParenToken,
			SyntaxKind.IdentifierToken,
			SyntaxKind.CloseParenToken,
			SyntaxKind.SemicolonToken,
			SyntaxKind.CloseBraceToken,
			SyntaxKind.CloseBraceToken,
			SyntaxKind.CloseBraceToken,
			SyntaxKind.CloseBraceToken
		};
		Assert.AreEqual(kinds.Count, tokens.Count);
		foreach (var (token, kind) in tokens.Zip(kinds, Tuple.Create))
			Assert.AreEqual(kind, token.Kind());
	}
}