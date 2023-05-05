using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Ulearn.Common.Extensions;

public static class DictionaryExtension
{
	public static DefaultDictionary<TKey, TValue> ToDefaultDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dict)
	{
		return new DefaultDictionary<TKey, TValue>(dict);
	}

	public static DefaultDictionary<TKey, TValue> ToDefaultDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dict, Func<TValue> defaultSelector)
	{
		return new DefaultDictionary<TKey, TValue>(dict, defaultSelector);
	}

	public static SortedDictionary<TKey, TValue> ToSortedDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dict)
	{
		return new SortedDictionary<TKey, TValue>(dict);
	}

	public static string ToQueryString(this IDictionary<string, string> dict)
	{
		var query = new NameValueCollection();
		foreach (var kvp in dict)
			query.Add(kvp.Key, kvp.Value);
		return query.ToQueryString();
	}
}