using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Ulearn.Common.Api.Helpers
{
	/* It's almost copy for HtmlExtensions class from Ulearn.Web/Extensions/HtmlExtensions.cs */
	public static class HtmlTransformations
	{
		// See https://en.wikipedia.org/wiki/List_of_Internet_top-level_domains for TLD list, we selected some of them
		private const string tldWhiteList = @"arpa|adult|aero|audio|best|blog|camera|camp|center|club|dance|data|education|email|game|global|hosting|jobs|live|map|mobi|photo|pizza|sale|shop|travel|video|wiki";

		private const string urlRegexS = @"(
											(
											 (?<protocol>(https?|ftps?)://|mailto:)
											 (?<hostnameOrIpAddress>
											  (([-a-z0-9_]*\.)+([a-z]*))
											  |
											  ((?:(?:25[0-5]|2[0-4]\d|[01]\d\d|\d?\d)(?(\.?\d)\.)){4})
											 )
											 |
											 (?<usernameAndPassword>
											  [-a-z0-9_.+]+
											  (:[-a-z0-9_.]+)?
											  @
											 )?
											 (?<hostnameOrIpAddress>
											  (([-a-z0-9_]{2,}\.)+([a-z]{2,3}(?<!txt|cs|min|max|add|io|pow)|" + tldWhiteList + @"))
											  |
											  ((?:(?:25[0-5]|2[0-4]\d|[01]\d\d|\d?\d)(?(\.?\d)\.)){4})
											 )
                                            )
											(?<port>:\d+)?
											(?<relativePath>
											 [/?]
											 (
											  [-a-z0-9._?,;'/\+&%$#=~!@]*
											  (?<pathInPairedBrackets>
											   \(
												[-a-z0-9._?,;'/\+&%$#=~!@]*
											   \)
											  )?
											  [-a-z0-9._?,;'/\+&%$#=~!@]*
											  # not all chars can be last in url
											  (?<=[-a-z0-9_'/\+&%$#=~@)]|&[a-z]+;)
											 )?
											)?
											# not all chars can be next after url
											(?=$|\n|[^a-zA-Z0-9])
										   )
										   ";

		public static readonly Regex urlRegex = new(urlRegexS, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

		public static string EncodeMultiLineText(string text, bool keepFirstSpaces = false)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			text = WebUtility.HtmlEncode(text);

			if (keepFirstSpaces)
				text = Regex.Replace(text, @"^\s+", m => string.Concat(Enumerable.Repeat("&nbsp;", m.Value.Length)), RegexOptions.Multiline);

			return text.Replace("\n", "<br/>").Replace("\r", "");
		}

		private static bool IsEmailUrl(string url)
		{
			return url.Contains('@') && !url.Contains('/');
		}

		public static string HighlightLink(string url)
		{
			var fullUrl = url;
			if (!url.Contains("://") && !url.StartsWith("mailto:"))
			{
				if (IsEmailUrl(url))
					fullUrl = "mailto:" + url;
				else
					fullUrl = "http://" + url;
			}

			return $"<a href=\"{EncodeAttribute(WebUtility.HtmlDecode(fullUrl))}\">{url}</a>";
		}

		/// <summary>
		///     Converts the specified attribute value to an HTML-encoded string. Use only in double quotes.
		///     Analog of
		///     [https://docs.microsoft.com/ru-ru/dotnet/api/system.web.httputility.htmlattributeencode?view=netframework-4.7.2]
		/// </summary>
		/// <returns>The HTML-encoded string. If the value parameter is null or empty, this method returns an empty string.</returns>
		/// <param name="attributeValue">The string to encode.</param>
		public static string EncodeAttribute(string attributeValue)
		{
			return string.IsNullOrEmpty(attributeValue) 
				? string.Empty 
				: attributeValue
					.Replace("&", "&amp;")
					.Replace("\"", "&quot;")
					.Replace("<", "&lt;")
					.Replace(">", "&gt;");
		}

		/// <summary>
		///     Find urls in html-encoded text and replace it with links
		/// </summary>
		/// <param name="htmlEncodedText">HTML-encoded text</param>
		/// <returns>HTML string with links</returns>
		public static string HighlightLinks(string htmlEncodedText)
		{
			return urlRegex.Replace(htmlEncodedText, match => HighlightLink(match.Groups[0].Value));
		}
	}
}