using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SyntaxNodeOrToken = Ulearn.Core.CSharp.Validators.IndentsValidation.SyntaxNodeOrToken;

namespace Ulearn.Core.CSharp.Validators
{
	public abstract class BaseStyleValidator : ICSharpSolutionValidator
	{
		public static FileLinePositionSpan GetSpan(SyntaxNode syntaxNode)
		{
			return syntaxNode.SyntaxTree.GetLineSpan(syntaxNode.Span);
		}

		public static FileLinePositionSpan GetSpan(SyntaxToken syntaxNode)
		{
			return syntaxNode.SyntaxTree!.GetLineSpan(syntaxNode.Span);
		}

		public static FileLinePositionSpan GetSpan(SyntaxNodeOrToken syntaxNode)
		{
			return syntaxNode.GetFileLinePositionSpan();
		}

		public static IEnumerable<SolutionStyleError> InspectAll<TNode>(SyntaxTree userSolution, SemanticModel semanticModel, Func<TNode, SemanticModel, IEnumerable<SolutionStyleError>> inspect)
			where TNode : SyntaxNode
		{
			var nodes = userSolution.GetRoot().DescendantNodes().OfType<TNode>();
			return nodes.SelectMany(node => inspect(node, semanticModel));
		}

		public static IEnumerable<SolutionStyleError> InspectAll<TNode>(SyntaxTree userSolution, Func<TNode, IEnumerable<SolutionStyleError>> inspect)
			where TNode : SyntaxNode
		{
			var nodes = userSolution.GetRoot().DescendantNodes().OfType<TNode>();
			return nodes.SelectMany(inspect);
		}

		public List<SolutionStyleError> FindErrors(string code)
		{
			var syntaxTree = CSharpSyntaxTree.ParseText(code);
			var compilation = CSharpCompilation.Create("MyCompilation", new[] { syntaxTree }, new[] { mscorlib });
			var semanticModel = compilation.GetSemanticModel(syntaxTree);
			return FindErrors(syntaxTree, semanticModel);
		}

		public abstract List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel);

		private static readonly PortableExecutableReference mscorlib =
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
	}
}