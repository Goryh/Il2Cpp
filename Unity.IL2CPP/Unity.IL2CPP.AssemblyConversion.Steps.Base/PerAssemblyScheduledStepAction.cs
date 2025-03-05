using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class PerAssemblyScheduledStepAction<TWorkerContext> : ScheduledItemsStepAction<TWorkerContext, AssemblyDefinition> where TWorkerContext : ITinyProfilerProvider
{
	protected override string ProfilerDetailsForItem(AssemblyDefinition workerItem)
	{
		return workerItem.Name.Name;
	}
}
