using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class ScheduledItemsStepFuncWithContinueAction<TWorkerContext, TWorkerItem, TWorkerResult> : BaseScheduledItemsStep<TWorkerContext, TWorkerItem> where TWorkerContext : ITinyProfilerProvider
{
	protected abstract string PostProcessingSectionName { get; }

	protected abstract TWorkerResult ProcessItem(TWorkerContext context, TWorkerItem item);

	public virtual void Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<TWorkerItem> items)
	{
		using (CreateProfilerSectionAroundScheduling(scheduler.SchedulingContext, scheduler.WorkIsDoneOnDifferentThread))
		{
			if (!Skip(scheduler.SchedulingContext))
			{
				scheduler.EnqueueItemsAndContinueWithResults<TWorkerContext, TWorkerItem, TWorkerResult, object>(scheduler.QueuingContext, items, WorkerWrapper, PostProcessWrapper, null);
			}
		}
	}

	private void PostProcessWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<TWorkerItem, TWorkerResult>>, object> workerData)
	{
		using (workerData.Context.TinyProfiler.Section(PostProcessingSectionName))
		{
			PostProcess(workerData.Context, workerData.Item);
		}
	}

	protected abstract void PostProcess(TWorkerContext context, ReadOnlyCollection<ResultData<TWorkerItem, TWorkerResult>> data);

	private TWorkerResult WorkerWrapper(WorkItemData<TWorkerContext, TWorkerItem, object> workerData)
	{
		using (CreateProfilerSectionForProcessItem(workerData.Context, workerData.Item))
		{
			return ProcessItem(workerData.Context, workerData.Item);
		}
	}
}
