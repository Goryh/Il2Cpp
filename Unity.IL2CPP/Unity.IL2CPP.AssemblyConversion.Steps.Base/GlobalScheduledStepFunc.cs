using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class GlobalScheduledStepFunc<TWorkerContext, TGlobalState, TResult> : BaseScheduledItemsStep<TWorkerContext, AssemblyDefinition> where TWorkerContext : ITinyProfilerProvider
{
	public ReadOnlyGlobalPendingResults<TResult> Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<AssemblyDefinition> items)
	{
		GlobalPendingResults<TResult> pendingResults = new GlobalPendingResults<TResult>();
		if (Skip(scheduler.SchedulingContext))
		{
			pendingResults.SetResults(CreateEmptyResult());
			return new ReadOnlyGlobalPendingResults<TResult>(pendingResults);
		}
		scheduler.Enqueue(scheduler.QueuingContext, items, WorkerWrapper, pendingResults);
		return new ReadOnlyGlobalPendingResults<TResult>(pendingResults);
	}

	protected abstract void ProcessItem(TWorkerContext context, AssemblyDefinition item, TGlobalState globalState);

	protected abstract TResult CreateEmptyResult();

	protected abstract TGlobalState CreateGlobalState(TWorkerContext context);

	protected abstract TResult GetResults(TWorkerContext context, TGlobalState globalState);

	private void WorkerWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<AssemblyDefinition>, GlobalPendingResults<TResult>> workerData)
	{
		using (workerData.Context.TinyProfiler.Section(StepUtilities.FormatAllStepName(Name)))
		{
			TResult result = ProcessAllItems(workerData.Context, workerData.Item);
			workerData.Tag.SetResults(result);
		}
	}

	protected virtual TResult ProcessAllItems(TWorkerContext context, ReadOnlyCollection<AssemblyDefinition> items)
	{
		TGlobalState globalState = CreateGlobalState(context);
		foreach (AssemblyDefinition assembly in items)
		{
			using (CreateProfilerSectionForProcessItem(context, assembly))
			{
				ProcessItem(context, assembly, globalState);
			}
		}
		return GetResults(context, globalState);
	}

	protected override string ProfilerDetailsForItem(AssemblyDefinition workerItem)
	{
		return workerItem.Name.Name;
	}
}
