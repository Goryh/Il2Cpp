using System.Collections.Generic;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly;

public class WindowsRuntimeDataCollection : PerAssemblyScheduledStepFunc<GlobalPrimaryCollectionContext, CollectedWindowsRuntimeData>
{
	protected override string Name => "Collect Windows Runtime Data";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override CollectedWindowsRuntimeData ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
	{
		PrimaryCollectionContext collectionContext = context.CreateCollectionContext();
		IWindowsRuntimeTypeWithNameCollector typeWithNameCollector = context.Collectors.WindowsRuntimeTypeWithNames;
		List<WindowsRuntimeFactoryData> windowsRuntimeFactories = new List<WindowsRuntimeFactoryData>();
		foreach (TypeDefinition type in item.GetAllTypes())
		{
			if (type.NeedsWindowsRuntimeFactory())
			{
				windowsRuntimeFactories.Add(new WindowsRuntimeFactoryData(type, context.Collectors.Types.Add(type)));
			}
			string fullName;
			if (type.IsExposedToWindowsRuntime())
			{
				if (type.HasGenericParameters || type.MetadataType != MetadataType.Class || type.IsInterface)
				{
					continue;
				}
				fullName = type.FullName;
			}
			else
			{
				TypeDefinition projectedToWindowsRuntime = context.Services.WindowsRuntime.ProjectToWindowsRuntime(type);
				if (type == projectedToWindowsRuntime)
				{
					continue;
				}
				fullName = projectedToWindowsRuntime.FullName;
			}
			typeWithNameCollector.AddWindowsRuntimeTypeWithName(collectionContext, type, fullName);
		}
		return new CollectedWindowsRuntimeData(windowsRuntimeFactories.ToSortedCollection(new WindowsRuntimeFactoryDataComparer()));
	}
}
