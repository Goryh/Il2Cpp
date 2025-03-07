using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.Awesome.Ordering;

public class MethodKeyOrderingComparer<TValue> : IComparer<KeyValuePair<MethodReference, TValue>>, IComparer<KeyValuePair<MethodDefinition, TValue>>, IComparer<KeyValuePair<GenericInstanceMethod, TValue>>
{
	public int Compare(KeyValuePair<MethodReference, TValue> x, KeyValuePair<MethodReference, TValue> y)
	{
		return x.Key.Compare(y.Key);
	}

	public int Compare(KeyValuePair<MethodDefinition, TValue> x, KeyValuePair<MethodDefinition, TValue> y)
	{
		return x.Key.Compare(y.Key);
	}

	public int Compare(KeyValuePair<GenericInstanceMethod, TValue> x, KeyValuePair<GenericInstanceMethod, TValue> y)
	{
		return x.Key.Compare(y.Key);
	}
}
