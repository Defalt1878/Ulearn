using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Slides.Blocks;
using Ulearn.Core.Model;

namespace Ulearn.Core.CSharp
{
	public class CsMembersExtractor : ISingleRegionExtractor
	{
		private readonly Dictionary<string, List<MemberDeclarationSyntax>> members;

		public CsMembersExtractor(string code)
		{
			var tree = CSharpSyntaxTree.ParseText(code);
			members = tree.GetRoot().GetMembers()
				.GroupBy(node => node.Identifier().ValueText)
				.ToDictionary(
					nodes => nodes.Key,
					nodes => nodes.ToList()
				);
		}

		public string GetRegion(Label label, bool withoutAttributes = false)
		{
			if (!members.ContainsKey(label.Name))
				return null;
			var nodes = members[label.Name];
			if (label.OnlyBody)
				return string.Join("\r\n\r\n", nodes.Select(GetBody));
			if (withoutAttributes)
				nodes = nodes.Select(node => node.WithoutAttributes()).ToList();
			return string.Join("\r\n\r\n", nodes.Select(node => node.ToPrettyString()));
		}

		private static string GetBody(SyntaxNode node)
		{
			return node.GetBody().ToFullString().RemoveCommonNesting();
		}
	}
}