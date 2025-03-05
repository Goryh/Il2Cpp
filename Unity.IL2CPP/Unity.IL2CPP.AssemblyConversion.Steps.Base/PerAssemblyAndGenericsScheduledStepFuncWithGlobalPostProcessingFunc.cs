using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class PerAssemblyAndGenericsScheduledStepFuncWithGlobalPostProcessingFunc<TWorkerContext, TGenericsItem, TWorkerResult, TPostProcessResult> : ScheduledTwoInItemsStepFuncWithContinueFunc<TWorkerContext, AssemblyDefinition, TGenericsItem, TWorkerResult, TPostProcessResult> where TWorkerContext : ITinyProfilerProvider
{
	protected override string ProfilerDetailsForItem(AssemblyDefinition workerItem)
	{
		return workerItem.Name.Name;
	}
}
