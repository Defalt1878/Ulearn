using System.Collections.Generic;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace System.Linq
{
	public static class EnumerableExtensions
	{
		public static IEnumerable<TSource> Deprecated_DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			var hash = new HashSet<TKey>();
			return source.Where(p => hash.Add(keySelector(p)));
		}

		public static IEnumerable<TSource> ExceptNulls<TSource>(this IEnumerable<TSource> source)
		{
			return source.Where(i => i is not null);
		}

		public static T SingleVerbose<T>(this IEnumerable<T> items, Func<T, bool> predicate, string predicateDescription = "")
		{
			var good = items.Where(predicate).Take(2).ToList();
			if (!good.Any())
				throw new InvalidOperationException(string.Join(" ", "not found item with", predicateDescription));
			if (good.Count > 1)
				throw new InvalidOperationException(string.Join(" ", "more than one item with", predicateDescription));
			return good[0];
		}

		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> items, Random random = null)
		{
			random = random ?? new Random();
			var copy = items.ToList();
			for (var i = 0; i < copy.Count; i++)
			{
				var nextIndex = random.Next(i, copy.Count);
				yield return copy[nextIndex];
				copy[nextIndex] = copy[i];
			}
		}

		public static IEnumerable<TSource> NotOfType<TSource, TSearch>(this IEnumerable<TSource> enumerable)
		{
			return enumerable.Where(item => !(item is TSearch));
		}

		/// <summary>Finds the index of the first item matching an expression in an enumerable.</summary>
		/// <param name="items">The enumerable to search.</param>
		/// <param name="predicate">The expression to test the items against.</param>
		/// <returns>The index of the first matching item, or -1 if no items match.</returns>
		public static int FindIndex<T>(this IEnumerable<T> items, Func<T, bool> predicate)
		{
			if (items is null)
				throw new ArgumentNullException(nameof(items));
			if (predicate is null)
				throw new ArgumentNullException(nameof(predicate));

			var index = 0;
			foreach (var item in items)
			{
				if (predicate(item))
					return index;
				index++;
			}

			return -1;
		}

		public static int FindIndex<T>(this IEnumerable<T> items, T element) where T : IEquatable<T>
		{
			return FindIndex(items, item => item.Equals(element));
		}

		[NotNull]
		public static IEnumerable<T> EmptyIfNull<T>([CanBeNull] this IEnumerable<T> items)
		{
			return items ?? Enumerable.Empty<T>();
		}

		public static Dictionary<TKey, TElement> ToDictSafe<TSource, TKey, TElement>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			Func<TSource, TElement> elementSelector)
		{
			var result = new Dictionary<TKey, TElement>();
			foreach (var e in source)
				result[keySelector(e)] = elementSelector(e);
			return result;
		}
	}
}