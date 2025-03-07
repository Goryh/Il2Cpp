using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP.DataModel.Awesome.Ordering;

public static class DictionaryExtensions
{
	public static ReadOnlyCollection<KeyValuePair<MethodReference, TValue>> ItemsSortedByKey<TValue>(this IEnumerable<KeyValuePair<MethodReference, TValue>> dict)
	{
		return dict.ToSortedCollection(new MethodKeyOrderingComparer<TValue>());
	}

	public static ReadOnlyCollection<KeyValuePair<MethodDefinition, TValue>> ItemsSortedByKey<TValue>(this IEnumerable<KeyValuePair<MethodDefinition, TValue>> dict)
	{
		return dict.ToSortedCollection(new MethodKeyOrderingComparer<TValue>());
	}

	public static ReadOnlyCollection<KeyValuePair<TypeReference, TValue>> ItemsSortedByKey<TValue>(this IEnumerable<KeyValuePair<TypeReference, TValue>> dict)
	{
		return dict.ToSortedCollection(new TypeKeyOrderingComparer<TValue>());
	}

	public static ReadOnlyCollection<KeyValuePair<TypeReference[], TValue>> ItemsSortedByKey<TValue>(this IEnumerable<KeyValuePair<TypeReference[], TValue>> dict)
	{
		return dict.ToSortedCollection(new TypeKeyOrderingComparer<TValue>());
	}

	public static ReadOnlyCollection<KeyValuePair<TypeDefinition, TValue>> ItemsSortedByKey<TValue>(this IEnumerable<KeyValuePair<TypeDefinition, TValue>> dict)
	{
		return dict.ToSortedCollection(new TypeKeyOrderingComparer<TValue>());
	}

	public static ReadOnlyCollection<KeyValuePair<AssemblyDefinition, TValue>> ItemsSortedByKey<TValue>(this IEnumerable<KeyValuePair<AssemblyDefinition, TValue>> dict)
	{
		return dict.ToSortedCollection(new AssemblyDefinitionDictionaryKeyOrderingComparer<TValue>());
	}
}
