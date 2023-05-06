using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ulearn.Common.Extensions;

namespace Ulearn.Core.CSharp.Validators.SpellingValidator
{
	public class SpellingValidator : BaseStyleValidator
	{
		private readonly CodeElementSpellChecker spellChecker = new();

		public override List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel)
		{
			return InspectAll<MethodDeclarationSyntax>(userSolution, InspectMethodsNamesAndArguments) // TODO: проверять имена классов?
				.Concat(InspectAll<LocalFunctionStatementSyntax>(userSolution, InspectLocalFunctionsNamesAndArguments))
				.Concat(InspectAll<VariableDeclarationSyntax>(userSolution, semanticModel, InspectVariablesNames))
				.Concat(InspectAll<PropertyDeclarationSyntax>(userSolution, InspectPropertiesNames))
				.Concat(InspectAll<ClassDeclarationSyntax>(userSolution, InspectClassNames))
				.Concat(InspectAll<StructDeclarationSyntax>(userSolution, InspectStructNames))
				.Concat(InspectAll<InterfaceDeclarationSyntax>(userSolution, InspectInterfaceNames))
				.ToList();
		}

		private IEnumerable<SolutionStyleError> InspectMethodsNamesAndArguments(MethodDeclarationSyntax methodDeclaration)
		{
			return InspectNamesAndArguments(methodDeclaration.Identifier, methodDeclaration.ReturnType,
				methodDeclaration.ParameterList);
		}

		private IEnumerable<SolutionStyleError> InspectLocalFunctionsNamesAndArguments(LocalFunctionStatementSyntax localFunctionStatement)
		{
			return InspectNamesAndArguments(localFunctionStatement.Identifier, localFunctionStatement.ReturnType,
				localFunctionStatement.ParameterList);
		}

		private IEnumerable<SolutionStyleError> InspectNamesAndArguments(SyntaxToken identifier, TypeSyntax returnType,
			ParameterListSyntax parameterList)
		{
			var errors = InspectMethodParameters(parameterList.Parameters);
			var errorInMethodName = CheckIdentifierNameForSpellingErrors(
				identifier,
				returnType.ToString());
			if (errorInMethodName != null)
				errors.Add(errorInMethodName);
			return errors;
		}

		private List<SolutionStyleError> InspectMethodParameters(SeparatedSyntaxList<ParameterSyntax> parameters)
		{
			return parameters
				.Select(InspectMethodParameter)
				.Where(err => err != null)
				.ToList();
		}

		private SolutionStyleError InspectMethodParameter(ParameterSyntax parameter)
		{
			var identifier = parameter.Identifier;
			return CheckIdentifierNameForSpellingErrors(identifier, parameter.Type?.ToString());
		}

		private IEnumerable<SolutionStyleError> InspectVariablesNames(VariableDeclarationSyntax variableDeclarationSyntax, SemanticModel semanticModel)
		{
			var typeInfo = semanticModel.GetTypeInfo(variableDeclarationSyntax.Type);
			return variableDeclarationSyntax.Variables.SelectMany(v => InspectVariablesNames(v, typeInfo));
		}

		private IEnumerable<SolutionStyleError> InspectClassNames(ClassDeclarationSyntax classDeclarationSyntax)
		{
			var errors = new List<SolutionStyleError>();
			var errorInName = CheckIdentifierNameForSpellingErrors(classDeclarationSyntax.Identifier);
			if (errorInName != null)
				errors.Add(errorInName);
			return errors;
		}

		private IEnumerable<SolutionStyleError> InspectStructNames(StructDeclarationSyntax structDeclarationSyntax)
		{
			var errors = new List<SolutionStyleError>();
			var errorInName = CheckIdentifierNameForSpellingErrors(structDeclarationSyntax.Identifier);
			if (errorInName != null)
				errors.Add(errorInName);
			return errors;
		}

		private IEnumerable<SolutionStyleError> InspectInterfaceNames(InterfaceDeclarationSyntax interfaceDeclarationSyntax)
		{
			var interfaceNameParts = interfaceDeclarationSyntax.Identifier.ValueText.SplitByCamelCase().ToList();
			var errors = new List<SolutionStyleError>();
			if (interfaceNameParts.First() != "I")
				errors.Add(new SolutionStyleError(
					StyleErrorType.Misspelling01,
					interfaceDeclarationSyntax.Identifier,
					interfaceNameParts.First(), "Имя интерфейса должно начинаться с I"));
			var errorInName = CheckIdentifierNameForSpellingErrors(interfaceDeclarationSyntax.Identifier);
			if (errorInName != null)
				errors.Add(errorInName);
			return errors;
		}

		private IEnumerable<SolutionStyleError> InspectVariablesNames(VariableDeclaratorSyntax variableDeclaratorSyntax, TypeInfo variableTypeInfo)
		{
			var errors = new List<SolutionStyleError>();
			var errorInVariable = CheckIdentifierNameForSpellingErrors(variableDeclaratorSyntax.Identifier, variableTypeInfo.Type?.Name);
			if (errorInVariable != null)
				errors.Add(errorInVariable);
			return errors;
		}

		private IEnumerable<SolutionStyleError> InspectPropertiesNames(PropertyDeclarationSyntax propertyDeclaration)
		{
			var errors = new List<SolutionStyleError>();
			var propertyType = propertyDeclaration.Type;
			var propertyTypeAsString = propertyType.ToString();
			var errorInPropertyName = CheckIdentifierNameForSpellingErrors(propertyDeclaration.Identifier, propertyTypeAsString);
			if (errorInPropertyName != null)
				errors.Add(errorInPropertyName);

			return errors;
		}

		private SolutionStyleError CheckIdentifierNameForSpellingErrors(SyntaxToken identifier, string typeAsString = null)
		{
			var identifierName = identifier.ValueText;
			return spellChecker.CheckIdentifierNameForSpellingErrors(identifierName, typeAsString)
				? new SolutionStyleError(StyleErrorType.Misspelling01, identifier, identifierName, "")
				: null;
		}
	}
}