using System;

namespace Ulearn.Common.Extensions;

public static class UriExtensions
{
	public static string AddQueryParameter(this string url, string name, string value)
	{
		var uriBuilder = new UriBuilder(url);
		var query = WebUtils.ParseQueryString(uriBuilder.Query);
		query[name] = value;
		uriBuilder.Query = query.ToString();
		return uriBuilder.ToString();
	}

	public static string RemoveQueryParameter(this string url, string name)
	{
		var uriBuilder = new UriBuilder(url);
		var query = WebUtils.ParseQueryString(uriBuilder.Query);
		query.Remove(name);
		uriBuilder.Query = query.ToString();
		return uriBuilder.ToString();
	}
}