using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Ulearn.Common;

public class DefaultDictionary<TKey, TValue> : Dictionary<TKey, TValue>
{
	public DefaultDictionary()
	{
	}

	public DefaultDictionary([NotNull] IDictionary<TKey, TValue> dict)
		: base(dict)
	{
	}

	public DefaultDictionary([NotNull] IDictionary<TKey, TValue> dict, Func<TValue> defaultSelector)
		: base(dict)
	{
		DefaultSelector = defaultSelector;
	}

	public DefaultDictionary(Func<TValue> defaultSelector)
	{
		DefaultSelector = defaultSelector;
	}

	private Func<TValue> DefaultSelector { get; }

	public new TValue this[TKey key]
	{
		[CollectionAccess(CollectionAccessType.UpdatedContent)]
		get
		{
			if (TryGetValue(key, out var value))
				return value;

			value = DefaultSelector is not null ? DefaultSelector() : GetEmptyValue();
			Add(key, value);
			return value;
		}
		[CollectionAccess(CollectionAccessType.ModifyExistingContent)]
		set => base[key] = value;
	}

	private static TValue GetEmptyValue()
	{
		if (typeof(TValue) == typeof(string))
			return default;
		return typeof(TValue).IsValueType ? default : Activator.CreateInstance<TValue>();
	}
}