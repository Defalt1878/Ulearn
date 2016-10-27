using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace uLearn.CSharp
{
	public class HasRecursionStyleValidator : BaseStyleValidator
	{
		protected override IEnumerable<string> ReportAllErrors(SyntaxTree userSolution)
		{
			throw new System.NotImplementedException();
		}
	}

	public class BlockLengthStyleValidator : BaseStyleValidator
	{
		private readonly int maxLen;

		public BlockLengthStyleValidator(int maxLen)
		{
			this.maxLen = maxLen > 0 ? maxLen : 25;
		}

		protected override IEnumerable<string> ReportAllErrors(SyntaxTree userSolution)
		{
			return InspectAll<BlockSyntax>(userSolution, Inspect);
		}

		private IEnumerable<string> Inspect(BlockSyntax block)
		{
			var startLine = GetSpan(block.OpenBraceToken).StartLinePosition.Line;
			var endLine = GetSpan(block.CloseBraceToken).EndLinePosition.Line;
			if (endLine - startLine >= maxLen)
				yield return Report(block.OpenBraceToken, "������� ������� ���� ����������. ����������� ������� ��� �� ��������������� ������");
		}
	}
}