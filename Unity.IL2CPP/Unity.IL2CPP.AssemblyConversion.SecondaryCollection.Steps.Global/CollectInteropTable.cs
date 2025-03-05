using System.Collections.Generic;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Global;

public class CollectInteropTable : SimpleScheduledStepFunc<GlobalSecondaryCollectionContext, ReadOnlyInteropTable>
{
	protected override string Name => "Collect Interop Tables";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override ReadOnlyInteropTable CreateEmptyResult()
	{
		return null;
	}

	protected override ReadOnlyInteropTable Worker(GlobalSecondaryCollectionContext context)
	{
		Dictionary<IIl2CppRuntimeType, InteropData> table = new Dictionary<IIl2CppRuntimeType, InteropData>(Il2CppRuntimeTypeEqualityComparer.Default);
		foreach (IIl2CppRuntimeType runtimeType in context.Results.PrimaryCollection.CCWMarshalingFunctions)
		{
			InteropData interopData = new InteropData();
			interopData.HasCreateCCWFunction = true;
			table.Add(runtimeType, interopData);
		}
		foreach (IIl2CppRuntimeType runtimeType2 in context.Results.PrimaryWrite.TypeMarshallingFunctions)
		{
			if (!table.TryGetValue(runtimeType2, out var interopData2))
			{
				interopData2 = new InteropData();
				table.Add(runtimeType2, interopData2);
			}
			interopData2.HasPInvokeMarshalingFunctions = true;
		}
		foreach (IIl2CppRuntimeType runtimeType3 in context.Results.PrimaryWrite.WrappersForDelegateFromManagedToNative)
		{
			if (!table.TryGetValue(runtimeType3, out var interopData3))
			{
				interopData3 = new InteropData();
				table.Add(runtimeType3, interopData3);
			}
			interopData3.HasDelegatePInvokeWrapperMethod = true;
		}
		foreach (IIl2CppRuntimeType runtimeType4 in context.Results.PrimaryWrite.InteropGuids)
		{
			if (!table.TryGetValue(runtimeType4, out var interopData4))
			{
				interopData4 = new InteropData();
				table.Add(runtimeType4, interopData4);
			}
			interopData4.HasGuid = true;
		}
		return new ReadOnlyInteropTable(table.ItemsSortedByKey());
	}
}
