using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class ChunkedItemsWithPostProcessingAction<TWorkerContext, TWorkerItem, TWorkerResult> : BaseScheduledItemsStep<TWorkerContext, ReadOnlyCollection<TWorkerItem>> where TWorkerContext : ITinyProfilerProvider
{
	protected abstract string PostProcessingSectionName { get; }

	protected abstract TWorkerResult ProcessItem(TWorkerContext context, ReadOnlyCollection<TWorkerItem> item);

	public void Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<TWorkerItem> items)
	{
		using (CreateProfilerSectionAroundScheduling(scheduler.SchedulingContext, scheduler.WorkIsDoneOnDifferentThread))
		{
			if (!Skip(scheduler.SchedulingContext))
			{
				scheduler.EnqueueItemsAndContinueWithResults<TWorkerContext, ReadOnlyCollection<TWorkerItem>, TWorkerResult, object>(scheduler.QueuingContext, Chunk(scheduler.SchedulingContext, items), WorkerWrapper, PostProcessWrapper, null);
			}
		}
	}

	protected abstract ReadOnlyCollection<ReadOnlyCollection<TWorkerItem>> Chunk(GlobalSchedulingContext context, ReadOnlyCollection<TWorkerItem> items);

	private void PostProcessWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<ReadOnlyCollection<TWorkerItem>, TWorkerResult>>, object> workerData)
	{
		using (workerData.Context.TinyProfiler.Section(PostProcessingSectionName))
		{
			PostProcess(workerData.Context, workerData.Item);
		}
	}

	protected abstract void PostProcess(TWorkerContext context, ReadOnlyCollection<ResultData<ReadOnlyCollection<TWorkerItem>, TWorkerResult>> data);

	private TWorkerResult WorkerWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<TWorkerItem>, object> workerData)
	{
		using (CreateProfilerSectionForProcessItem(workerData.Context, workerData.Item))
		{
			return ProcessItem(workerData.Context, workerData.Item);
		}
	}
}
