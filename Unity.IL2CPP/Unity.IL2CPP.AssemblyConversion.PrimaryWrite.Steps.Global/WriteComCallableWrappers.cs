using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.SourceWriters;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.Global;

public class WriteComCallableWrappers : SimpleScheduledStep<GlobalWriteContext>
{
	protected override string Name => "Write Com Callable Wrappers";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override void Worker(GlobalWriteContext context)
	{
		ComCallableWrappersSourceWriter.EnqueueWrites(context.CreateSourceWritingContext());
	}
}
