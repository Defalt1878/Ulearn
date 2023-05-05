using System.Diagnostics.Contracts;

namespace Ulearn.Common.Extensions;

public static class StringHasherExtensions
{
	/// <summary>
	///     Returns the hash code for this string. This implementation is copied from .Net 4.7 sources for x64 machines
	/// </summary>
	/// <param name="str"></param>
	/// <returns>A 32-bit signed integer hash code.</returns>
	public static unsafe int GetStableHashCode(this string str)
	{
		fixed (char* src = str)
		{
			Contract.Assert(src[str.Length] == '\0', "src[str.Length] == '\\0'");
			Contract.Assert((int)src % 4 == 0, "Managed string should start at 4 bytes boundary");

			var hash1 = 5381;
			var hash2 = hash1;

			int c;
			var s = src;
			while ((c = s[0]) != 0)
			{
				hash1 = ((hash1 << 5) + hash1) ^ c;
				c = s[1];
				if (c == 0)
					break;
				hash2 = ((hash2 << 5) + hash2) ^ c;
				s += 2;
			}

			return hash1 + hash2 * 1566083941;
		}
	}
}