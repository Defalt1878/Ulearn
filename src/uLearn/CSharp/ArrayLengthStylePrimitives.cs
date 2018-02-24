﻿using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace uLearn.CSharp
{
	public static class ArrayLengthStylePrimitives
	{
		public static SyntaxNode GetParentCycle(this SyntaxNode syntaxNode)
		{
			var ancestors = syntaxNode.Ancestors().ToList();
			return ancestors.FirstOrDefault(c => c.IsCycle());
		}

		public static bool IsCycle(this SyntaxNode syntaxNode)
		{
			return syntaxNode is ForStatementSyntax
					|| syntaxNode is WhileStatementSyntax
					|| syntaxNode is DoStatementSyntax
					|| syntaxNode is ForEachStatementSyntax;
		}

		public static bool ContainsAssignmentOf(this StatementSyntax statementSyntax, string variableName) // TODO: посмотреть насчёт синтаксической модели и передачи списка узлов
		{ // TODO: сделать проверку на массивы (не здесь)
			if (!statementSyntax.IsCycle())
				return false;

			var assignments = statementSyntax
				.DescendantNodes()
				.OfType<AssignmentExpressionSyntax>()
				.ToList();
			var declarations = statementSyntax
				.DescendantNodes()
				.OfType<VariableDeclaratorSyntax>()
				.ToList();
			
			foreach (var assignment in assignments)
			{
				if (assignment.Left.ToString() == variableName)
					return true;
			}
			foreach (var declaration in declarations)
			{
				if (declaration.Identifier.Text == variableName)
					return true;
			}
			
			return false; // TODO
		}
	}
}