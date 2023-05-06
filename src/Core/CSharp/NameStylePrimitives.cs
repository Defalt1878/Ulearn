using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ulearn.Core.CSharp
{
	public static class NameStylePrimitives
	{
		public static bool IsSingleWordGerundIdentifier(this string name)
		{
			return name.EndsWith("ing") && !name.Skip(1).Any(char.IsUpper) && !name.Contains("_");
		}

		public static bool IsSingleWordIonIdentifier(this string name)
		{
			return name.EndsWith("ion") && !name.Skip(1).Any(char.IsUpper) && !name.Contains("_");
		}

		public static bool IsNoArgsSetter(this MethodDeclarationSyntax method)
		{
			var name = method.Identifier.Text;
			var isSetter = name.StartsWith("Set", StringComparison.OrdinalIgnoreCase);
			var noArgs = !method.ParameterList.Parameters.Any();
			return noArgs && isSetter;
		}

		public static bool IsVoidGetter(this MethodDeclarationSyntax method)
		{
			var name = method.Identifier.Text;
			var isGetter = name.StartsWith("Get", StringComparison.OrdinalIgnoreCase);
			return method.IsVoid() && isGetter;
		}

		public static bool IsNoArgsSetter(this LocalFunctionStatementSyntax method)
		{
			var name = method.Identifier.Text;
			var isSetter = name.StartsWith("Set", StringComparison.OrdinalIgnoreCase);
			var noArgs = !method.ParameterList.Parameters.Any();
			return noArgs && isSetter;
		}

		public static bool IsVoidGetter(this LocalFunctionStatementSyntax method)
		{
			var name = method.Identifier.Text;
			var isGetter = name.StartsWith("Get", StringComparison.OrdinalIgnoreCase);
			return method.IsVoid() && isGetter;
		}
	}
}