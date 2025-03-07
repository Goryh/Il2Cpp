using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.Awesome.Ordering;

public class TypeKeyOrderingComparer<TValue> : IComparer<KeyValuePair<TypeReference, TValue>>, IComparer<KeyValuePair<TypeDefinition, TValue>>, IComparer<KeyValuePair<GenericInstanceType, TValue>>, IComparer<KeyValuePair<TypeReference[], TValue>>
{
	public int Compare(KeyValuePair<TypeReference, TValue> x, KeyValuePair<TypeReference, TValue> y)
	{
		return x.Key.Compare(y.Key);
	}

	public int Compare(KeyValuePair<TypeDefinition, TValue> x, KeyValuePair<TypeDefinition, TValue> y)
	{
		return x.Key.Compare(y.Key);
	}

	public int Compare(KeyValuePair<GenericInstanceType, TValue> x, KeyValuePair<GenericInstanceType, TValue> y)
	{
		return x.Key.Compare(y.Key);
	}

	public int Compare(KeyValuePair<TypeReference[], TValue> x, KeyValuePair<TypeReference[], TValue> y)
	{
		return x.Key.Compare(y.Key);
	}
}
