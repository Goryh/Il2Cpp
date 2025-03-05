using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class GlobalScheduledStepAction<TWorkerContext, TGlobalState> : BaseScheduledItemsStep<TWorkerContext, AssemblyDefinition> where TWorkerContext : ITinyProfilerProvider
{
	protected abstract void ProcessItem(TWorkerContext context, AssemblyDefinition item, TGlobalState globalState);

	protected abstract TGlobalState CreateGlobalState(TWorkerContext context);

	public void Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<AssemblyDefinition> items)
	{
		if (!Skip(scheduler.SchedulingContext))
		{
			scheduler.Enqueue<TWorkerContext, ReadOnlyCollection<AssemblyDefinition>, object>(scheduler.QueuingContext, items, WorkerWrapper, null);
		}
	}

	private void WorkerWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<AssemblyDefinition>, object> workerData)
	{
		using (workerData.Context.TinyProfiler.Section(StepUtilities.FormatAllStepName(Name)))
		{
			ProcessAllItems(workerData.Context, workerData.Item);
		}
	}

	protected virtual void ProcessAllItems(TWorkerContext context, ReadOnlyCollection<AssemblyDefinition> items)
	{
		TGlobalState globalState = CreateGlobalState(context);
		foreach (AssemblyDefinition assembly in items)
		{
			using (CreateProfilerSectionForProcessItem(context, assembly))
			{
				ProcessItem(context, assembly, globalState);
			}
		}
	}

	protected override string ProfilerDetailsForItem(AssemblyDefinition workerItem)
	{
		return workerItem.Name.Name;
	}
}
