using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ulearn.Common;

public class LruCache<TKey, TValue>
{
	private readonly Dictionary<TKey, LinkedListNode<LruCacheItem<TKey, TValue>>> cache = new();
	private readonly int capacity;
	private readonly LinkedList<LruCacheItem<TKey, TValue>> lastUsedItems = new();
	private readonly TimeSpan maxLifeTime;

	public LruCache(int capacity, TimeSpan maxLifeTime)
	{
		this.capacity = capacity;
		this.maxLifeTime = maxLifeTime;
	}

	public LruCache(int capacity)
		: this(capacity, TimeSpan.MaxValue)
	{
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	public bool TryGet(TKey key, out TValue value)
	{
		value = default;

		if (!cache.TryGetValue(key, out var node))
			return false;

		if (IsItemTooOld(node.Value))
		{
			lastUsedItems.Remove(node);
			cache.Remove(key);
			return false;
		}

		value = node.Value.Value;
		lastUsedItems.Remove(node);
		lastUsedItems.AddLast(node);
		return true;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	public bool Replace(TKey key, TValue value)
	{
		if (!cache.TryGetValue(key, out var node))
			return false;

		lastUsedItems.Remove(node);
		cache.Remove(key);

		Add(key, value);
		return true;
	}

	private bool IsItemTooOld(LruCacheItem<TKey, TValue> cacheItem)
	{
		var now = DateTime.Now;
		return now - cacheItem.AddingTime > maxLifeTime;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	public void Add(TKey key, TValue val)
	{
		if (cache.Count >= capacity)
			RemoveFirst();

		var cacheItem = new LruCacheItem<TKey, TValue>(key, val);
		var node = new LinkedListNode<LruCacheItem<TKey, TValue>>(cacheItem);
		lastUsedItems.AddLast(node);
		cache.Add(key, node);
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	public void Clear()
	{
		cache.Clear();
		lastUsedItems.Clear();
	}

	private void RemoveFirst()
	{
		var node = lastUsedItems.First;
		lastUsedItems.RemoveFirst();

		/* Remove from cache */
		cache.Remove(node!.Value.Key);
	}
}

internal class LruCacheItem<TKey, TValue>
{
	public readonly TKey Key;
	public readonly TValue Value;
	public readonly DateTime AddingTime;

	public LruCacheItem(TKey k, TValue v)
	{
		Key = k;
		Value = v;
		AddingTime = DateTime.Now;
	}
}