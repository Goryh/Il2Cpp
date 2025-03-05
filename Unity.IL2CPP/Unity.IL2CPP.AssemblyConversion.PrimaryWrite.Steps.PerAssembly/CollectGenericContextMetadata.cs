using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.PerAssembly;

public class CollectGenericContextMetadata : PerAssemblyScheduledStepFunc<GlobalWriteContext, GenericContextCollection>
{
	protected override string Name => "Collect Generic Context Metadata";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override GenericContextCollection ProcessItem(GlobalWriteContext context, AssemblyDefinition item)
	{
		return GenericContextCollector.Collect(context, item, context.Results.PrimaryCollection.GenericSharingAnalysis);
	}
}
