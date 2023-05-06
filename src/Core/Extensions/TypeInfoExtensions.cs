using Microsoft.CodeAnalysis;

namespace Ulearn.Core.Extensions
{
	public static class TypeInfoExtensions
	{
		public static bool IsPrimitive(this TypeInfo typeInfo)
		{
			return typeInfo.Type?.SpecialType switch
			{
				SpecialType.System_Boolean or
					SpecialType.System_Byte or
					SpecialType.System_SByte or
					SpecialType.System_Int16 or
					SpecialType.System_UInt16 or
					SpecialType.System_Int32 or
					SpecialType.System_UInt32 or
					SpecialType.System_Int64 or
					SpecialType.System_UInt64 or
					SpecialType.System_IntPtr or
					SpecialType.System_UIntPtr or
					SpecialType.System_Char or
					SpecialType.System_Double or
					SpecialType.System_Single => true,
				_ => false
			};
		}
	}
}