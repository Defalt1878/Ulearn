using System;
using System.Collections.Generic;

namespace Ulearn.Core.Extensions
{
	public static class EnumerableExtensions
	{
		public static TSource Deprecated_MaxBy<TSource, TProperty>(this IEnumerable<TSource> source, Func<TSource, TProperty> selector, Comparer<TProperty> comparer)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (selector == null)
				throw new ArgumentNullException(nameof(selector));
			if (comparer == null)
				throw new ArgumentNullException(nameof(comparer));

			using var iterator = source.GetEnumerator();
			if (!iterator.MoveNext())
				throw new InvalidOperationException();

			var max = iterator.Current;
			var maxValue = selector(max);

			while (iterator.MoveNext())
			{
				var current = iterator.Current;
				var currentValue = selector(current);

				if (comparer.Compare(currentValue, maxValue) > 0)
				{
					max = current;
					maxValue = currentValue;
				}
			}

			return max;
		}

		public static TSource Deprecated_MaxBy<TSource, TProperty>(this IEnumerable<TSource> source, Func<TSource, TProperty> selector)
		{
			return source.Deprecated_MaxBy(selector, Comparer<TProperty>.Default);
		}
	}
}