using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP.Contexts.Components.Base;

public static class DataStructureMergingExtensions
{
	public static void Merge<T>(this HashSet<T> parent, HashSet<T> forked)
	{
		parent.UnionWith(forked);
	}

	public static void MergeNoConflictsAllowed<TKey, TValue>(this Dictionary<TKey, TValue> parent, Dictionary<TKey, TValue> forked, Func<TValue, TValue, bool> valuesAreEqual)
	{
		foreach (KeyValuePair<TKey, TValue> item in forked)
		{
			if (parent.TryGetValue(item.Key, out var parentValue))
			{
				if (!valuesAreEqual(parentValue, item.Value))
				{
					throw new InvalidOperationException($"Conflict for `{item.Key}`.  Parent has `{parentValue}` while forked had {item.Value}");
				}
			}
			else
			{
				parent[item.Key] = item.Value;
			}
		}
	}

	public static void MergeWithMergeConflicts<TKey, TValue>(this Dictionary<TKey, TValue> parent, Dictionary<TKey, TValue> forked, Func<TValue, TValue, TValue> mergeValues)
	{
		foreach (KeyValuePair<TKey, TValue> item in forked)
		{
			if (parent.TryGetValue(item.Key, out var parentValue))
			{
				parent[item.Key] = mergeValues(parentValue, item.Value);
			}
			else
			{
				parent[item.Key] = item.Value;
			}
		}
	}

	public static ReadOnlyDictionary<TKey, TValue> MergeNoConflictsAllowed<TKey, TValue>(this IEnumerable<IEnumerable<KeyValuePair<TKey, TValue>>> items)
	{
		Dictionary<TKey, TValue> merged = new Dictionary<TKey, TValue>();
		foreach (IEnumerable<KeyValuePair<TKey, TValue>> item2 in items)
		{
			foreach (KeyValuePair<TKey, TValue> item in item2)
			{
				merged.Add(item.Key, item.Value);
			}
		}
		return merged.AsReadOnly();
	}
}
