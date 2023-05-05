using System;
using System.Collections.Generic;
using System.Linq;

namespace Ulearn.Common.Extensions
{
	public class ItemWithIndex<T>
	{
		public ItemWithIndex(T item, int index)
		{
			Item = item;
			Index = index;
		}

		public T Item { get; }
		public int Index { get; }

		public void Deconstruct(out int index, out T item)
		{
			index = Index;
			item = Item;
		}
	}

	// ReSharper disable once InconsistentNaming
	public static class IEnumerableExtensions
	{
		public static IEnumerable<ItemWithIndex<T>> Enumerate<T>(this IEnumerable<T> collection, int start = 0)
		{
			var index = start;

			foreach (var element in collection)
				yield return new ItemWithIndex<T>(element, index++);
		}

		public static string Join(this IEnumerable<object> collection, string separator)
		{
			return string.Join(separator, collection.Select(o => o.ToString()));
		}

		public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
		{
			foreach (var obj in collection)
				action(obj);
		}
	}
}