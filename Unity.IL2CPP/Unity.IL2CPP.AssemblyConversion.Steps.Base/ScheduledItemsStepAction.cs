using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.Contexts.Scheduling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class ScheduledItemsStepAction<TWorkerContext, TWorkerItem> : BaseScheduledItemsStep<TWorkerContext, TWorkerItem> where TWorkerContext : ITinyProfilerProvider
{
	protected abstract void ProcessItem(TWorkerContext context, TWorkerItem item);

	public void Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<TWorkerItem> items)
	{
		using (CreateProfilerSectionAroundScheduling(scheduler.SchedulingContext, scheduler.WorkIsDoneOnDifferentThread))
		{
			if (!Skip(scheduler.SchedulingContext))
			{
				scheduler.EnqueueItems<TWorkerContext, TWorkerItem, object>(scheduler.QueuingContext, items, WorkerWrapper, null);
			}
		}
	}

	private void WorkerWrapper(WorkItemData<TWorkerContext, TWorkerItem, object> workerData)
	{
		using (CreateProfilerSectionForProcessItem(workerData.Context, workerData.Item))
		{
			ProcessItem(workerData.Context, workerData.Item);
		}
	}
}
