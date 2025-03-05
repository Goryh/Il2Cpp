using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.SourceWriters;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.Global;

public class WriteGenericComDefinitions : SimpleScheduledStep<GlobalWriteContext>
{
	protected override string Name => "Write Generic Com Definitions";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override void Worker(GlobalWriteContext context)
	{
		GenericComDefinitionSourceWriter.EnqueueWrites(context.CreateSourceWritingContext(), context.Results.PrimaryCollection.Generics.TypeDeclarations);
	}
}
