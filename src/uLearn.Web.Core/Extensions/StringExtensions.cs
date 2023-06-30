
using Microsoft.AspNetCore.Http.Extensions;

namespace uLearn.Web.Core.Extensions;

public static class StringExtensions
{
	public static string TruncateHtmlWithEllipsis(this string s, int maxLength, string ellipsis = "...")
	{
		if (maxLength < 0)
			return s;

		if (ellipsis.Length > maxLength)
			throw new ArgumentOutOfRangeException("length", maxLength, "length must be at least as long as ellipsis.");

		var inTag = false;
		var realCharsCount = 0;
		var tags = new Stack<string>();
		var currentTag = "";
		var isClosingTag = false;
		for (var i = 0; i < s.Length; i++)
		{
			if (s[i] == '<' && !inTag)
			{
				currentTag = "";
				inTag = true;
				isClosingTag = false;
				continue;
			}

			if (s[i] == '>' && inTag)
			{
				if (isClosingTag && tags.Count > 0)
					tags.Pop();
				if (!isClosingTag)
					tags.Push(currentTag);
				inTag = false;
			}

			if (inTag)
			{
				if (s[i] == '/' && s[i - 1] == '<')
					isClosingTag = true;
				else
					currentTag += s[i];
				continue;
			}

			// Ignore HTML entities. HTML entity is all between & and ;
			if (s[i] == '&')
			{
				const int maxEntityLength = 10;
				int entityEnd;
				for (entityEnd = i + 1; entityEnd < i + maxEntityLength && entityEnd < s.Length; entityEnd++)
					if (s[entityEnd] == ';')
						break;
				if (entityEnd < s.Length && s[entityEnd] == ';')
				{
					i = entityEnd;
					continue;
				}
			}

			realCharsCount++;
			if (realCharsCount == maxLength)
			{
				var closingTags = "";
				while (tags.Count > 0)
					closingTags += $"</{tags.Pop()}>";
				return s.Substring(0, i + 1) + ellipsis + closingTags;
			}
		}

		return s;
	}

	public static int IndexOfFirstLetter(this string s)
	{
		for (var i = 0; i < s.Length; i++)
			if (char.IsLetter(s[i]))
				return i;
		return -1;
	}

	public static char FindFirstLetter(this string s, char ifNotFound)
	{
		var index = s.IndexOfFirstLetter();
		if (index == -1)
			return ifNotFound;
		return s[index];
	}

	// /* TODO (andgein): Move to ControllerBase? */
	public static bool IsLocalUrl(this string url, HttpRequest request)
	{
		if (string.IsNullOrEmpty(url))
			return false;
		
		var uri = new Uri(request.GetDisplayUrl());
	
		if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
			return uri != null && string.Equals(uri.Host, absoluteUri.Host, StringComparison.OrdinalIgnoreCase);
	
		var isLocal = !url.StartsWith("http:", StringComparison.OrdinalIgnoreCase)
					&& !url.StartsWith("https:", StringComparison.OrdinalIgnoreCase)
					&& Uri.IsWellFormedUriString(url, UriKind.Relative);
		
		return isLocal;
	}
}