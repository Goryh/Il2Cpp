using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Global;

public class CollectGenericInstances : SimpleScheduledStepFunc<GlobalSecondaryCollectionContext, ReadOnlyGenericInstanceTable>
{
	protected override string Name => "Collect Generic Instances";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override ReadOnlyGenericInstanceTable CreateEmptyResult()
	{
		return new ReadOnlyGenericInstanceTable(new Dictionary<IIl2CppRuntimeType[], uint>().AsReadOnly(), new List<KeyValuePair<IIl2CppRuntimeType[], uint>>().AsReadOnly());
	}

	protected override ReadOnlyGenericInstanceTable Worker(GlobalSecondaryCollectionContext context)
	{
		ReadOnlyDictionary<IIl2CppRuntimeType[], uint> readOnlyDictionary = Il2CppGenericInstCollectorComponent.Collect(context.CreateCollectionContext());
		ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType[], uint>> sortedValues = readOnlyDictionary.ItemsSortedByValue();
		return new ReadOnlyGenericInstanceTable(readOnlyDictionary, sortedValues);
	}
}
