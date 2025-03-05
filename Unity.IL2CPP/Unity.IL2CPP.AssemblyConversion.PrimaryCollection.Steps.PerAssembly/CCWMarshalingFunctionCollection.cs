using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly;

public class CCWMarshalingFunctionCollection : PerAssemblyScheduledStepAction<GlobalPrimaryCollectionContext>
{
	protected override string Name => "Collect CCW Marshaling Functions";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override void ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
	{
		PrimaryCollectionContext collectionContext = context.CreateCollectionContext();
		ICCWMarshallingFunctionCollector collector = context.Collectors.CCWMarshallingFunctionCollector;
		foreach (TypeDefinition type in item.GetAllTypes())
		{
			if (MethodWriter.TypeMethodsCanBeDirectlyCalled(collectionContext, type) && (type.NeedsComCallableWrapper(collectionContext) || NeedsComCallableWrapperForMarshaledType(collectionContext, type)))
			{
				collector.Add(collectionContext, type);
			}
		}
	}

	internal static bool NeedsComCallableWrapperForMarshaledType(ReadOnlyContext context, TypeReference type)
	{
		MarshalType[] marshalTypesForMarshaledType = MarshalingUtils.GetMarshalTypesForMarshaledType(context, type);
		for (int i = 0; i < marshalTypesForMarshaledType.Length; i++)
		{
			if (marshalTypesForMarshaledType[i] == MarshalType.WindowsRuntime && type.IsDelegate && (!(type is TypeSpecification) || type is GenericInstanceType))
			{
				return true;
			}
		}
		return false;
	}
}
