using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Attributes;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly;

public class AttributeSupportCollection : PerAssemblyScheduledStepFunc<GlobalPrimaryCollectionContext, ReadOnlyCollectedAttributeSupportData>
{
	protected override string Name => "Collecting Attribute Data";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override ReadOnlyCollectedAttributeSupportData ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
	{
		return new ReadOnlyCollectedAttributeSupportData(AttributeCollection.BuildAttributeCollection(context, item));
	}
}
