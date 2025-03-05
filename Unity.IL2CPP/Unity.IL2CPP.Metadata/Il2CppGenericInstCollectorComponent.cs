using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata;

public static class Il2CppGenericInstCollectorComponent
{
	public static ReadOnlyDictionary<IIl2CppRuntimeType[], uint> Collect(SecondaryCollectionContext context)
	{
		Dictionary<IIl2CppRuntimeType[], uint> allInstances = new Dictionary<IIl2CppRuntimeType[], uint>(Il2CppRuntimeTypeArrayEqualityComparer.Default);
		foreach (IIl2CppRuntimeType runtimeType in context.Global.Results.PrimaryWrite.Types.SortedItems)
		{
			if (runtimeType.Type.IsGenericInstance)
			{
				Il2CppGenericInstanceRuntimeType genericRuntimeType = (Il2CppGenericInstanceRuntimeType)runtimeType;
				AddChecked(allInstances, genericRuntimeType.GenericArguments);
			}
		}
		foreach (Il2CppMethodSpec genericMethod in context.Global.Results.PrimaryWrite.GenericMethods.SortedKeys)
		{
			AddChecked(allInstances, genericMethod.MethodGenericInstanceData);
			AddChecked(allInstances, genericMethod.TypeGenericInstanceData);
		}
		return allInstances.AsReadOnly();
	}

	private static void AddChecked(Dictionary<IIl2CppRuntimeType[], uint> allInstances, IIl2CppRuntimeType[] genericArguments)
	{
		if (genericArguments != null && !allInstances.ContainsKey(genericArguments))
		{
			allInstances.Add(genericArguments, (uint)allInstances.Count);
		}
	}
}
