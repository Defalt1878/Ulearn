using System.Collections.Specialized;

namespace Ulearn.Common.Extensions;

public static class NameValueCollectionExtensions
{
	public static string ToQueryString(this NameValueCollection collection)
	{
		var qs = WebUtils.ParseQueryString("");
		qs.Add(collection);
		return qs.ToString();
	}
}