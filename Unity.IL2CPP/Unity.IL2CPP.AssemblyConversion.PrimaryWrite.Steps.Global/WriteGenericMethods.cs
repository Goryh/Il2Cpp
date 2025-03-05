using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.SourceWriters;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.Global;

public class WriteGenericMethods : SimpleScheduledStep<GlobalWriteContext>
{
	protected override string Name => "Write Generic Methods";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override void Worker(GlobalWriteContext context)
	{
		GenericMethodSourceWriter.EnqueueWrites(context.CreateSourceWritingContext(), context.Results.PrimaryCollection.Generics.Methods);
	}
}
