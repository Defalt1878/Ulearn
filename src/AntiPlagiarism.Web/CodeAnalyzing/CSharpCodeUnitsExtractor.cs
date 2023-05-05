using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AntiPlagiarism.Web.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ulearn.Common.Extensions;

namespace AntiPlagiarism.Web.CodeAnalyzing;

public class CSharpCodeUnitsExtractor
{
	public List<CodeUnit> Extract(string program)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(program);
		var syntaxTreeRoot = syntaxTree.GetRoot() as CompilationUnitSyntax;
		var codeUnits = GetCodeUnitsFrom(syntaxTreeRoot, ImmutableList<CodePathPart>.Empty).ToList();

		var tokens = syntaxTreeRoot.GetTokens();
		var tokenIndexByPosition = tokens
			.Enumerate()
			.ToDictionary(
				t => t.Item.SpanStart,
				t => t.Index
			);

		foreach (var unit in codeUnits)
			unit.FirstTokenIndex = tokenIndexByPosition[unit.Tokens[0].Position];

		return codeUnits;
	}

	private IEnumerable<CodeUnit> GetCodeUnitsFrom(CSharpSyntaxNode node, ImmutableList<CodePathPart> codePath)
	{
		return node switch
		{
			CompilationUnitSyntax syntax => GetCodeUnitsFromChildren(syntax, codePath, z => z.Members),
			NamespaceDeclarationSyntax syntax => GetCodeUnitsFromChildren(syntax, codePath, z => z.Members),
			TypeDeclarationSyntax syntax => GetCodeUnitsFromChildren(syntax, codePath, z => z.Members),
			PropertyDeclarationSyntax syntax => GetCodeUnitsFromChildren(syntax, codePath, PropertyEnumerator),
			BaseMethodDeclarationSyntax syntax => GetCodeUnitsFromChildren(syntax, codePath, MethodEnumerator),
			AccessorDeclarationSyntax syntax => GetCodeUnitFrom(syntax, codePath, z => z.Body),
			ArrowExpressionClauseSyntax syntax => GetCodeUnitFrom(syntax, codePath, z => z.Expression),
			BlockSyntax syntax => GetCodeUnitFrom(syntax, codePath, z => z),
			ConstructorInitializerSyntax syntax => GetCodeUnitFrom(syntax, codePath, z => z),
			_ => Enumerable.Empty<CodeUnit>()
		};
	}

	private IEnumerable<CodeUnit> GetCodeUnitsFromChildren<T>(
		T node,
		ImmutableList<CodePathPart> codePath,
		Func<T, IEnumerable<CSharpSyntaxNode>> childrenFunc
	)
		where T : CSharpSyntaxNode
	{
		var nodeName = GetNodeName(node);
		var currentCodePath = codePath.Add(new CodePathPart(node, nodeName));

		return childrenFunc(node)
			.SelectMany(child => GetCodeUnitsFrom(child, currentCodePath));
	}

	private static IEnumerable<CodeUnit> GetCodeUnitFrom<T>(
		T node,
		ImmutableList<CodePathPart> codePath,
		Func<T, CSharpSyntaxNode> entryFunc
	)
		where T : CSharpSyntaxNode
	{
		var entry = entryFunc(node);
		if (entry is null)
			return Enumerable.Empty<CodeUnit>();

		var nodeName = GetNodeName(node);
		var currentCodePath = new CodePath(codePath.Add(new CodePathPart(node, nodeName)));

		var tokens = entry.GetTokens().Select(token => new CSharpToken(token));
		return new[] { new CodeUnit(currentCodePath, tokens) };
	}

	private static IEnumerable<CSharpSyntaxNode> PropertyEnumerator(PropertyDeclarationSyntax syntax)
	{
		if (syntax.AccessorList is not null)
		{
			foreach (var e in syntax.AccessorList.Accessors)
				yield return e;
		}
		else if (syntax.ExpressionBody is not null)
		{
			yield return syntax.ExpressionBody;
		}
	}

	private static IEnumerable<CSharpSyntaxNode> MethodEnumerator(BaseMethodDeclarationSyntax syntax)
	{
		if (syntax is ConstructorDeclarationSyntax constructor)
			yield return constructor.Initializer;

		if (syntax.Body is not null)
			yield return syntax.Body;
		else if (syntax.ExpressionBody is not null)
			yield return syntax.ExpressionBody;
	}

	public static string GetNodeName(SyntaxNode node) =>
		node switch
		{
			CompilationUnitSyntax => "ROOT",
			NamespaceDeclarationSyntax syntaxNode => syntaxNode.Name.ToString(),
			BaseTypeDeclarationSyntax syntaxNode => syntaxNode.Identifier.ValueText,
			PropertyDeclarationSyntax syntaxNode => syntaxNode.Identifier.ValueText,
			BaseMethodDeclarationSyntax syntaxNode => GetNodeName(syntaxNode),
			CSharpSyntaxNode syntaxNode => syntaxNode.Kind().ToString(),
			_ => throw new InvalidOperationException("node should be CSharpSyntaxNode")
		};

	private static string GetNodeName(BaseMethodDeclarationSyntax node) =>
		node switch
		{
			ConstructorDeclarationSyntax syntaxNode => syntaxNode.Identifier.ToString(),
			ConversionOperatorDeclarationSyntax syntaxNode =>
				"Conversion-" + syntaxNode.Type + "-from-" +
				string.Join("-", syntaxNode.ParameterList.Parameters.Select(p => p.Type)),
			DestructorDeclarationSyntax syntaxNode => syntaxNode.Identifier.ToString(),
			MethodDeclarationSyntax syntaxNode => syntaxNode.Identifier.ToString(),
			OperatorDeclarationSyntax syntaxNode => "Operator" + syntaxNode.OperatorToken,
			CSharpSyntaxNode syntaxNode => syntaxNode.Kind().ToString(),
		};
}