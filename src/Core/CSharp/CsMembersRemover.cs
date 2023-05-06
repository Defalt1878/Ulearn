using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ulearn.Core.Courses.Slides.Blocks;
using Ulearn.Core.Model;

namespace Ulearn.Core.CSharp
{
	public class CsMembersRemover : IRegionRemover
	{
		public const string Pragma = "\r\n#line 1\r\n";

		public string Remove(string code, IEnumerable<Label> labels, out IEnumerable<Label> notRemoved)
		{
			var tree = CSharpSyntaxTree.ParseText(code).GetRoot();
			notRemoved = labels.Where(label => !Remove(label, ref tree)).ToList();
			return tree.ToFullString();
		}

		public string RemoveSolution(string code, Label label, out int index)
		{
			var tree = CSharpSyntaxTree.ParseText(code).GetRoot();
			var solution = tree.GetMembers().FirstOrDefault(node => node.Identifier().ValueText == label.Name);
			if (solution == null)
			{
				index = -1;
				return code;
			}

			if (label.OnlyBody)
			{
				var body = solution.GetBody();
				index = body.FullSpan.Start;
				tree = tree.RemoveNodes(body, SyntaxRemoveOptions.KeepNoTrivia);
			}
			else
			{
				index = solution.Span.Start;
				tree = tree.RemoveNode(solution, SyntaxRemoveOptions.KeepExteriorTrivia);
			}

			code = tree!.ToFullString();
			return code;
		}

		public string Prepare(string code)
		{
			return RemoveUsings(code);
		}

		private static bool Remove(Label label, ref SyntaxNode tree)
		{
			var members = tree.GetMembers()
				.Where(node => node.Identifier().ValueText == label.Name)
				.ToList();
			if (!members.Any())
				return false;
			tree = label.OnlyBody 
				? tree.RemoveNodes(members.SelectMany(syntax => syntax.GetBody()), SyntaxRemoveOptions.KeepNoTrivia) 
				: tree.RemoveNodes(members, SyntaxRemoveOptions.KeepExteriorTrivia);
			return true;
		}

		public static string RemoveUsings(string code)
		{
			var tree = CSharpSyntaxTree.ParseText(code).GetRoot();
			var usings = tree.DescendantNodes().OfType<UsingDirectiveSyntax>();
			return tree.RemoveNodes(usings, SyntaxRemoveOptions.KeepExteriorTrivia)!.ToFullString();
		}
	}
}