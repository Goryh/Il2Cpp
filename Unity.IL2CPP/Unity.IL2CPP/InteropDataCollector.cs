using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP;

internal static class InteropDataCollector
{
	public static ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType, InteropData>> Collect(SourceWritingContext context)
	{
		Dictionary<IIl2CppRuntimeType, InteropData> table = new Dictionary<IIl2CppRuntimeType, InteropData>(Il2CppRuntimeTypeEqualityComparer.Default);
		foreach (IIl2CppRuntimeType runtimeType in context.Global.Results.PrimaryCollection.CCWMarshalingFunctions)
		{
			InteropData interopData = new InteropData();
			interopData.HasCreateCCWFunction = true;
			table.Add(runtimeType, interopData);
		}
		foreach (IIl2CppRuntimeType runtimeType2 in context.Global.Results.PrimaryWrite.TypeMarshallingFunctions)
		{
			if (!table.TryGetValue(runtimeType2, out var interopData2))
			{
				interopData2 = new InteropData();
				table.Add(runtimeType2, interopData2);
			}
			interopData2.HasPInvokeMarshalingFunctions = true;
		}
		foreach (IIl2CppRuntimeType runtimeType3 in context.Global.Results.PrimaryWrite.WrappersForDelegateFromManagedToNative)
		{
			if (!table.TryGetValue(runtimeType3, out var interopData3))
			{
				interopData3 = new InteropData();
				table.Add(runtimeType3, interopData3);
			}
			interopData3.HasDelegatePInvokeWrapperMethod = true;
		}
		foreach (IIl2CppRuntimeType runtimeType4 in context.Global.Results.PrimaryWrite.InteropGuids)
		{
			if (!table.TryGetValue(runtimeType4, out var interopData4))
			{
				interopData4 = new InteropData();
				table.Add(runtimeType4, interopData4);
			}
			interopData4.HasGuid = true;
		}
		return table.ItemsSortedByKey();
	}
}
