using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Ulearn.Common.Extensions
{
	public static class CollectionExtensions
	{
		public static TOutput Call<TInput, TOutput>(this TInput input, Func<TInput, TOutput> f)
			where TInput : class
			where TOutput : class
		{
			return input is null ? null : f(input);
		}

		public static TOutput Call<TInput, TOutput>(this TInput input, Func<TInput, TOutput> f, TOutput defaultValue)
			where TInput : class
		{
			return input is null ? defaultValue : f(input);
		}

		public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			TValue v;
			if (dictionary.TryGetValue(key, out v))
				return v;
			throw new KeyNotFoundException("Key " + key + " not found in dictionary");
		}

		/* Returns default(TValue) if key not found */
		public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			return dictionary.TryGetValue(key, out var v) ? v : default;
		}

		public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
		{
			return dictionary.TryGetValue(key, out var v) ? v : defaultValue;
		}

		[CanBeNull]
		public static List<T> NullIfEmpty<T>(this List<T> items)
		{
			return items?.Count == 0 ? null : items;
		}
	}
}