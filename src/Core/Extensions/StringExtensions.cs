using System;
using System.Text.RegularExpressions;

namespace Ulearn.Core.Extensions
{
	public static class StringExtensions
	{
		public static string TruncateWithEllipsis(this string s, int maxLength, string ellipsis = "...")
		{
			if (maxLength < 0)
				return s;

			if (ellipsis.Length > maxLength)
				throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, @"length must be at least as long as ellipsis.");

			if (s.Length > maxLength - ellipsis.Length)
				return s[..(maxLength - ellipsis.Length)].TrimEnd() + ellipsis;

			return s;
		}

		/* Warning: this does not work for all cases and should not be used to process untrusted user input. */
		public static string StripHtmlTags(this string input)
		{
			return Regex.Replace(input, "<.*?>", string.Empty);
		}
	}
}