using System.Linq;
using Ulearn.Common.Extensions;

namespace Ulearn.Core.CSharp
{
	public static class SpellingValidatorPrimitives
	{
		public static string MakeTypeNameAbbreviation(this string typeName)
		{
			return string.Join("", typeName.SplitByCamelCase().Select(word => word[0]));
		}
	}
}