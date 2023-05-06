using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ulearn.Core.CSharp.Validators
{
	public class NamingCaseStyleValidator : BaseNamingChecker
	{
		protected override IEnumerable<SolutionStyleError> InspectName(SyntaxToken identifier)
		{
			var name = identifier.Text;
			if (string.IsNullOrEmpty(name) || name.All(c => c == '_'))
				yield break;
			var mustStartWithUpper = MustStartWithUpper(identifier.Parent);
			var mustStartWithLower = MustStartWithLower(identifier.Parent);
			var isUpper = char.IsUpper(name[0]);
			var isLower = char.IsLower(name[0]) ||
						(name[0] == '_' && name.Length > 1 && char.IsLower(name[1]));

			if (mustStartWithLower && !isLower)
				yield return new SolutionStyleError(StyleErrorType.NamingCase01, identifier);
			if (mustStartWithUpper && !isUpper)
				yield return new SolutionStyleError(StyleErrorType.NamingCase02, identifier);
		}

		private static bool MustStartWithUpper(SyntaxNode node)
		{
			return
				node is BaseTypeDeclarationSyntax or TypeParameterSyntax or EnumMemberDeclarationSyntax ||
				(node is MethodDeclarationSyntax methodDeclaration && methodDeclaration.Modifiers.Any(t => t.IsKind(SyntaxKind.PublicKeyword))) ||
				(node is VariableDeclaratorSyntax variableDeclarator && MustStartWithUpper(variableDeclarator)) ||
				(node is ParameterSyntax && node.Parent?.Parent is RecordDeclarationSyntax);
		}

		private static bool MustStartWithUpper(VariableDeclaratorSyntax variableDeclarator)
		{
			var field = AsField(variableDeclarator);
			if (field == null) return false;
			// Публичные поля и константы → с большой
			var isStatic = field.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
			var isReadonly = field.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));
			var isConstant = field.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword));
			var isPublic = field.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
			// статические ридонли могут быть какие угодно.
			return (isPublic || isConstant) && !(isStatic && isReadonly);
		}

		private static bool MustStartWithLower(VariableDeclaratorSyntax variableDeclarator)
		{
			var field = AsField(variableDeclarator);
			if (field == null) return true;
			var isStatic = field.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
			var isReadonly = field.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));
			var isConstant = field.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword));
			var isPrivate = field.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)) ||
							!field.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword));
			// статические ридонли могут быть какие угодно.
			return isPrivate && !isConstant && !(isStatic && isReadonly);
		}

		private static BaseFieldDeclarationSyntax AsField(SyntaxNode variableDeclarator)
		{
			// Первый родитель, но не выше блока.
			var parent = variableDeclarator.GetParents().FirstOrDefault(p => p is BaseFieldDeclarationSyntax or BlockSyntax);
			return parent as BaseFieldDeclarationSyntax;
		}

		private static bool MustStartWithLower(SyntaxNode node)
		{
			return
				(node is ParameterSyntax && node.Parent?.Parent is not RecordDeclarationSyntax) ||
				(node is VariableDeclaratorSyntax syntax && MustStartWithLower(syntax));
		}
	}
}