using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Global;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public class ReadOnlyInvokerCollection
{
	private const int kMethodIndexInvalid = -1;

	private readonly ReadOnlyDictionary<InvokerSignature, int> _runtimeInvokerData;

	public readonly ReadOnlyCollection<InvokerSignature> SortedSignatures;

	public ReadOnlyInvokerCollection(Dictionary<InvokerSignature, int> runtimeInvokerData, ReadOnlyCollection<InvokerSignature> sortedSignatures)
	{
		_runtimeInvokerData = runtimeInvokerData.AsReadOnly();
		SortedSignatures = sortedSignatures;
	}

	public int GetIndex(ReadOnlyContext context, MethodReference method)
	{
		if (!CollectInvokers.ShouldCollectInvoker(context, method))
		{
			return -1;
		}
		InvokerSignature data = InvokerSignature.Create(context, method);
		if (!_runtimeInvokerData.TryGetValue(data, out var index))
		{
			return -1;
		}
		return index;
	}
}
