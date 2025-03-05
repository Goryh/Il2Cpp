using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class PerAssemblyScheduledStepFunc<TWorkerContext, TWorkerResult> : ScheduledItemsStepFunc<TWorkerContext, AssemblyDefinition, TWorkerResult, PerAssemblyPendingResults<TWorkerResult>, ReadOnlyPerAssemblyPendingResults<TWorkerResult>> where TWorkerContext : ITinyProfilerProvider
{
	protected override PerAssemblyPendingResults<TWorkerResult> CreateScheduleResult()
	{
		return new PerAssemblyPendingResults<TWorkerResult>();
	}

	protected override ReadOnlyPerAssemblyPendingResults<TWorkerResult> CreateReadOnlyPendingResults(PerAssemblyPendingResults<TWorkerResult> pendingResults)
	{
		return new ReadOnlyPerAssemblyPendingResults<TWorkerResult>(pendingResults);
	}

	protected override string ProfilerDetailsForItem(AssemblyDefinition workerItem)
	{
		return workerItem.Name.Name;
	}
}
