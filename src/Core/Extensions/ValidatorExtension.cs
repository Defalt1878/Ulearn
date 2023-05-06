using System.Linq;

namespace Ulearn.Core.Extensions
{
	internal static class ValidatorExtension
	{
		public static SolutionBuildResult ValidateSolution(this ISolutionValidator validator, string userWrittenCode, string fullCodeFile)
		{
			string message;

			if ((message = validator.FindSyntaxError(fullCodeFile)) != null ||
				(message = validator.FindStrictValidatorErrors(userWrittenCode, fullCodeFile)) != null)
				return new SolutionBuildResult(fullCodeFile, message);

			var styleErrors = validator.FindValidatorErrors(userWrittenCode, fullCodeFile);
			return styleErrors.Any()
				? new SolutionBuildResult(fullCodeFile, styleErrors: styleErrors)
				: new SolutionBuildResult(fullCodeFile);
		}

		public static SolutionBuildResult ValidateSingleFileSolution(this ISolutionValidator validator, string userWrittenCode, string fullCodeFile)
		{
			string message;
			return (message = validator.FindFullSourceError(userWrittenCode)) != null
				? new SolutionBuildResult(fullCodeFile, message)
				: validator.ValidateSolution(userWrittenCode, fullCodeFile);
		}
	}
}