using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Ulearn.Common
{
	public static class StringUtils
	{
		private static readonly Random random;
		private const string alphanumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

		static StringUtils()
		{
			random = new Random();
		}

		public static string GenerateAlphanumericString(int length)
		{
			return new string(
				Enumerable.Repeat(alphanumericChars, length).Select(s => s[random.Next(s.Length)]).ToArray()
			);
		}

		public static string GenerateSecureAlphanumericString(int length)
		{
			var chars = alphanumericChars.ToCharArray();
			var randomBytes = new byte[length];
#pragma warning disable SYSLIB0023
			using (var crypto = new RNGCryptoServiceProvider())
				crypto.GetNonZeroBytes(randomBytes);
#pragma warning restore SYSLIB0023

			var result = new StringBuilder(length);
			foreach (var randomByte in randomBytes)
				result.Append(chars[randomByte % chars.Length]);
			return result.ToString();
		}
	}
}