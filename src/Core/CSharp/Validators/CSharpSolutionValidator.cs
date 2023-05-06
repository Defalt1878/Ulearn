using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Ulearn.Core.CSharp.Validators.IndentsValidation;
using Ulearn.Core.CSharp.Validators.VerbInMethodNameValidation;
using Vostok.Logging.Abstractions;

namespace Ulearn.Core.CSharp.Validators
{
	public class CSharpSolutionValidator : ISolutionValidator
	{
		private static ILog Log => LogProvider.Get().ForContext(typeof(CSharpSolutionValidator));

		private readonly List<ICSharpSolutionValidator> validators = new()
		{
			new NotEmptyCodeValidator(),
			new BlockLengthStyleValidator(),
			new LineLengthStyleValidator(),
			new NamingCaseStyleValidator(),
			new VerbInMethodNameValidator(),
			new RedundantIfStyleValidator(),
			new NamingStyleValidator(),
			new ExponentiationValidator(),
			new BoolCompareValidator(),
			new ArrayLengthStyleValidator(),
			new ExcessLinesValidator(),
			new RefArgumentsValidator(),
			new VarInVariableDeclarationValidator(),
			new BracketValidator(),
			new RedundantElseValidator(),
			new SpellingValidator.SpellingValidator(),
			new IndentsValidator()
		};

		public CSharpSolutionValidator(bool removeDefaults = false)
		{
			if (removeDefaults)
				validators.Clear();
		}

		public void AddValidator(ICSharpSolutionValidator validator)
		{
			validators.RemoveAll(item => item.GetType() == validator.GetType());
			validators.Add(validator);
		}

		public string FindFullSourceError(string userCode)
		{
			userCode = userCode.Trim();
			if (userCode.StartsWith("using") || userCode.StartsWith("namespace"))
				return "Не нужно писать весь исходный файл целиком — пишите только метод / класс, который необходим в задаче.";
			return null;
		}

		public string FindSyntaxError(string solution)
		{
			var diagnostics = CSharpSyntaxTree.ParseText(solution).GetDiagnostics();
			var error = diagnostics.FirstOrDefault();
			return error?.ToString();
		}

		public List<SolutionStyleError> FindValidatorErrors(string userCode, string solution)
		{
			var solutionTree = CSharpSyntaxTree.ParseText(userCode);
			try
			{
				var compilation = CSharpCompilation.Create("MyCompilation", new[] { solutionTree }, new[] { mscorlib });
				var semanticModel = compilation.GetSemanticModel(solutionTree);
				return validators
					.Where(v => !(v is IStrictValidator))
					.SelectMany(v => v.FindErrors(solutionTree, semanticModel))
					.ToList();
			}
			catch (Exception e)
			{
				Log.Error(e, "Can't run style validators");
				return new List<SolutionStyleError>();
			}
		}

		public string FindStrictValidatorErrors(string userCode, string solution)
		{
			try
			{
				var solutionTree = CSharpSyntaxTree.ParseText(userCode);
				var compilation = CSharpCompilation.Create("MyCompilation", new[] { solutionTree }, new[] { mscorlib });
				var semanticModel = compilation.GetSemanticModel(solutionTree);
				return validators
					.Where(v => v is IStrictValidator)
					.SelectMany(v => v.FindErrors(solutionTree, semanticModel))
					.FirstOrDefault()
					?.Message;
			}
			catch (Exception e)
			{
				return e.Message;
			}
		}

		private static readonly PortableExecutableReference mscorlib =
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
	}
}