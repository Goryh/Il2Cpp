using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Results.Phases;

public class SharedMethodCollection : TableResults<MethodReference, ReadOnlyCollection<MethodReference>>
{
	public SharedMethodCollection(ReadOnlyDictionary<MethodReference, ReadOnlyCollection<MethodReference>> methodsSharedFrom, ReadOnlyCollection<KeyValuePair<MethodReference, ReadOnlyCollection<MethodReference>>> sortedItems, ReadOnlyCollection<MethodReference> sortedKeys)
		: base(methodsSharedFrom, sortedItems, sortedKeys)
	{
	}

	public ReadOnlyCollection<MethodReference> GetMethodsSharedFrom(MethodReference sharedMethod)
	{
		if (TryGetValue(sharedMethod, out var value))
		{
			return value;
		}
		return new ReadOnlyCollection<MethodReference>(new MethodReference[0]);
	}
}
