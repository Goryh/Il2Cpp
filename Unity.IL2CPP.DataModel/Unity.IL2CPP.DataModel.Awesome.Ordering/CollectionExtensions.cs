using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP.DataModel.Awesome.Ordering;

public static class CollectionExtensions
{
	private class MethodReferenceOrderingComparerBy<T> : IComparer<T>
	{
		private readonly Func<T, MethodReference> _selector;

		public MethodReferenceOrderingComparerBy(Func<T, MethodReference> selector)
		{
			_selector = selector;
		}

		public int Compare(T x, T y)
		{
			return _selector(x).Compare(_selector(y));
		}
	}

	public static ReadOnlyCollection<TypeReference> ToSortedCollection(this IEnumerable<TypeReference> set)
	{
		return set.ToSortedCollection(new TypeOrderingComparer());
	}

	public static ReadOnlyCollection<GenericInstanceType> ToSortedCollection(this IEnumerable<GenericInstanceType> set)
	{
		return set.ToSortedCollection(new TypeOrderingComparer());
	}

	public static ReadOnlyCollection<TypeReference[]> ToSortedCollection(this IEnumerable<TypeReference[]> set)
	{
		return set.ToSortedCollection(new TypeOrderingComparer());
	}

	public static ReadOnlyCollection<TypeDefinition> ToSortedCollection(this IEnumerable<TypeDefinition> set)
	{
		return set.ToSortedCollection(new TypeOrderingComparer());
	}

	public static ReadOnlyCollection<GenericInstanceMethod> ToSortedCollection(this IEnumerable<GenericInstanceMethod> set)
	{
		return set.ToSortedCollection(new MethodOrderingComparer());
	}

	public static ReadOnlyCollection<T> ToSortedCollection<T>(this IEnumerable<T> set) where T : MethodReference
	{
		return set.ToSortedCollection(new MethodOrderingComparer());
	}

	public static ReadOnlyCollection<T> ToSortedCollectionBy<T>(this IEnumerable<T> set, Func<T, MethodReference> selector)
	{
		return set.ToSortedCollection(new MethodReferenceOrderingComparerBy<T>(selector));
	}

	public static ReadOnlyCollection<AssemblyDefinition> ToSortedCollection(this IEnumerable<AssemblyDefinition> set)
	{
		return set.ToSortedCollection(new AssemblyOrderingComparer());
	}

	public static ReadOnlyCollection<T> ToSortedCollection<T>(this IEnumerable<T> set, IComparer<T> comparer)
	{
		List<T> list = new List<T>(set);
		list.Sort(comparer);
		return list.AsReadOnly();
	}
}
