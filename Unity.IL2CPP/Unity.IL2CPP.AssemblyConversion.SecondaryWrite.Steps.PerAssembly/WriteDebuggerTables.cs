using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Debugger;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.PerAssembly;

public class WriteDebuggerTables : PerAssemblyScheduledStepAction<GlobalWriteContext>
{
	protected override string Name => "Write Debugger Table";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return !context.Parameters.EnableDebugger;
	}

	protected override void ProcessItem(GlobalWriteContext context, AssemblyDefinition item)
	{
		DebugWriter.WriteDebugMetadata(context.CreateSourceWritingContext(), item, (SequencePointCollector)context.PrimaryCollectionResults.SequencePoints.GetProvider(item), context.PrimaryCollectionResults.CatchPoints.GetCollector(item));
	}
}
