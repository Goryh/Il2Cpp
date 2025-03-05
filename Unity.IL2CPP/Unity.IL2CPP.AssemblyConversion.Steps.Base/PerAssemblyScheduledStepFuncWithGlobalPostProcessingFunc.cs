using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class PerAssemblyScheduledStepFuncWithGlobalPostProcessingFunc<TWorkerContext, TWorkerResult, TPostProcessResult> : ScheduledItemsStepFuncWithContinueFunc<TWorkerContext, AssemblyDefinition, TWorkerResult, TPostProcessResult> where TWorkerContext : ITinyProfilerProvider
{
	protected override string ProfilerDetailsForItem(AssemblyDefinition workerItem)
	{
		return workerItem.Name.Name;
	}
}
