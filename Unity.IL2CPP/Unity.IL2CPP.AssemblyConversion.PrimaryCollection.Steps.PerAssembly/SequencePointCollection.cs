using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Debugger;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly;

public class SequencePointCollection : PerAssemblyScheduledStepFunc<GlobalPrimaryCollectionContext, ISequencePointProvider>
{
	protected override string Name => "Debugger Sequence Points";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return !context.Parameters.EnableDebugger;
	}

	protected override ISequencePointProvider ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
	{
		SequencePointCollector sequencePointCollector = new SequencePointCollector();
		foreach (TypeDefinition allType in item.GetAllTypes())
		{
			foreach (MethodDefinition method in allType.Methods)
			{
				MethodWriter.CollectSequencePoints(context.CreateCollectionContext(), method, sequencePointCollector);
			}
		}
		sequencePointCollector.Complete();
		return sequencePointCollector;
	}
}
