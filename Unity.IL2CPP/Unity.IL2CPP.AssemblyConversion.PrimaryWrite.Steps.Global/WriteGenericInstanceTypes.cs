using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.SourceWriters;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.Global;

public class WriteGenericInstanceTypes : SimpleScheduledStep<GlobalWriteContext>
{
	protected override string Name => "Write Generic Instance Types";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override void Worker(GlobalWriteContext context)
	{
		GenericInstanceTypeSourceWriter.EnqueueWrites(context.CreateSourceWritingContext(), "Generics", context.Results.PrimaryCollection.Generics.Types);
	}
}
