using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ulearn.Common.Extensions;

namespace Ulearn.Core.CSharp
{
	public static class SyntaxExtensions
	{
		public static bool IsVoid(this MethodDeclarationSyntax method)
		{
			var predefinedTypeSyntax = method.ReturnType as PredefinedTypeSyntax;
			return predefinedTypeSyntax != null && predefinedTypeSyntax.Keyword.IsKind(SyntaxKind.VoidKeyword);
		}

		public static bool IsVoid(this LocalFunctionStatementSyntax method)
		{
			var predefinedTypeSyntax = method.ReturnType as PredefinedTypeSyntax;
			return predefinedTypeSyntax != null && predefinedTypeSyntax.Keyword.IsKind(SyntaxKind.VoidKeyword);
		}

		public static IEnumerable<MemberDeclarationSyntax> GetMembers(this SyntaxNode node)
		{
			return node.DescendantNodes()
				.OfType<MemberDeclarationSyntax>()
				.Where(n => n is BaseTypeDeclarationSyntax or MethodDeclarationSyntax or DelegateDeclarationSyntax);
		}

		public static SyntaxList<SyntaxNode> GetBody(this SyntaxNode node)
		{
			if (node is BaseMethodDeclarationSyntax method)
			{
				var body = method.Body;
				if (body != null)
					return body.Statements;
				return new SyntaxList<SyntaxNode>();
			}

			if (node is TypeDeclarationSyntax type)
				return type.Members;

			if (node is LocalFunctionStatementSyntax localFunction)
			{
				var body = localFunction.Body;
				if (body != null)
					return body.Statements;
				return new SyntaxList<SyntaxNode>();
			}

			return new SyntaxList<SyntaxNode>();
		}

		public static IEnumerable<SyntaxNode> GetParents(this SyntaxNode node)
		{
			while (node.Parent != null)
				yield return node = node.Parent;
		}

		public static IEnumerable<SyntaxNode> GetParents(this SyntaxToken token)
		{
			return new[] { token.Parent }.Concat(token.Parent.GetParents());
		}

		public static IEnumerable<SyntaxNode> GetParents(this SyntaxTrivia trivia)
		{
			return trivia.Token.GetParents();
		}

		public static SyntaxToken Identifier(this MemberDeclarationSyntax syntax)
		{
			if (syntax is FieldDeclarationSyntax fieldDeclarationSyntax)
				return fieldDeclarationSyntax.Declaration.Variables.First().Identifier;

			return ((dynamic)syntax).Identifier;
		}

		public static bool HasAttribute<TAttr>(this MemberDeclarationSyntax node) where TAttr : Attribute
		{
			return node.GetAttributes<TAttr>().Any();
		}

		public static object GetObjArgument(this AttributeSyntax attribute, int index)
		{
			var expr = (LiteralExpressionSyntax)attribute.ArgumentList?.Arguments[index].Expression;
			return expr?.Token.Value;
		}

		public static string GetArgument(this AttributeSyntax attribute, int index)
		{
			return (string)attribute.GetObjArgument(index);
		}

		private static SyntaxList<AttributeListSyntax> AttributeLists(this MemberDeclarationSyntax node)
		{
			return (SyntaxList<AttributeListSyntax>)((dynamic)node).AttributeLists;
		}

		public static IEnumerable<AttributeSyntax> GetAttributes<TAttr>(this MemberDeclarationSyntax node)
			where TAttr : Attribute
		{
			var attrShortName = GetAttributeShortName<TAttr>();
			return node.AttributeLists()
				.SelectMany(a => a.Attributes)
				.Where(a => a.Name.ToString() == attrShortName);
		}

		public static string GetAttributeShortName<TAttr>()
		{
			var attrName = typeof(TAttr).Name;
			return attrName.EndsWith("Attribute") ? attrName[..^"Attribute".Length] : attrName;
		}

		public static MethodDeclarationSyntax WithoutAttributes(this MethodDeclarationSyntax node)
		{
			return node.WithAttributeLists(new SyntaxList<AttributeListSyntax>());
		}

		public static MemberDeclarationSyntax WithoutAttributes(this MemberDeclarationSyntax node)
		{
			return (MemberDeclarationSyntax)((dynamic)node).WithAttributeLists(new SyntaxList<AttributeListSyntax>());
		}

		private static string PrettyString(SyntaxNode node, SyntaxToken tokenToAlignLeft)
		{
			var bodyNestingSize = node.SyntaxTree.GetLineSpan(tokenToAlignLeft.Span).StartLinePosition.Character;
			return (new string('\t', bodyNestingSize) + node).RemoveCommonNesting();
		}

		private static string PrettyString(BaseMethodDeclarationSyntax node)
		{
			var body = node.Body;
			return body is null
				? node.ToFullString().RemoveCommonNesting()
				: PrettyString(node, body.CloseBraceToken);
		}

		private static string PrettyString(BaseTypeDeclarationSyntax node)
		{
			return PrettyString(node, node.CloseBraceToken);
		}

		private static string PrettyString(MemberDeclarationSyntax node)
		{
			return node is FieldDeclarationSyntax
				? node.ToString().RemoveCommonNesting()
				: node.ToFullString().RemoveCommonNesting();
		}

		public static string ToPrettyString(this SyntaxNode node)
		{
			return node switch
			{
				BaseMethodDeclarationSyntax syntax => PrettyString(syntax),
				BaseTypeDeclarationSyntax syntax => PrettyString(syntax),
				MemberDeclarationSyntax syntax => PrettyString(syntax),
				_ => throw new ArgumentOutOfRangeException(nameof(node), node, null)
			};
		}

		public static string ToNotIndentedString(this SyntaxNode node)
		{
			return node.ToString().RemoveCommonNesting();
		}

		public static MethodDeclarationSyntax TransformExercise(this MethodDeclarationSyntax method)
		{
			return method
				.WithoutAttributes()
				.WithBody(method.Body?.WithStatements(new SyntaxList<StatementSyntax>()));
		}

		public static int GetLine(this SyntaxToken token)
		{
			return token.GetLocation().GetLineSpan().StartLinePosition.Line;
		}

		public static IEnumerable<BracesPair> BuildBracesPairs(this SyntaxTree tree)
		{
			var braces = tree.GetRoot().DescendantTokens()
				.Where(t => t.IsKind(SyntaxKind.OpenBraceToken) || t.IsKind(SyntaxKind.CloseBraceToken));
			var openBracesStack = new Stack<SyntaxToken>();
			foreach (var brace in braces)
				if (brace.IsKind(SyntaxKind.OpenBraceToken))
					openBracesStack.Push(brace);
				else
					yield return new BracesPair(openBracesStack.Pop(), brace);
		}
	}
}