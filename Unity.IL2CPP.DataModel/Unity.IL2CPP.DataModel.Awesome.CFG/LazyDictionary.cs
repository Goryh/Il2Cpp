using System;
using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.Awesome.CFG;

internal class LazyDictionary<K, V>
{
	private readonly Func<V> _createValue;

	private Dictionary<K, V> _items = new Dictionary<K, V>();

	public V this[K key]
	{
		get
		{
			if (_items.TryGetValue(key, out var val))
			{
				return val;
			}
			_items.Add(key, val = _createValue());
			return val;
		}
	}

	public LazyDictionary(Func<V> createValue)
	{
		_createValue = createValue;
	}

	public bool TryGetValue(K key, out V value)
	{
		return _items.TryGetValue(key, out value);
	}
}
