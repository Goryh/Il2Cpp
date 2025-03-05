using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class ChunkedItemsWithPostProcessingFunc<TWorkerContext, TWorkerItem, TWorkerResult, TPostProcessResult> : BaseScheduledItemsStep<TWorkerContext, ReadOnlyCollection<TWorkerItem>> where TWorkerContext : ITinyProfilerProvider
{
	protected abstract string PostProcessingSectionName { get; }

	protected abstract TWorkerResult ProcessItem(TWorkerContext context, ReadOnlyCollection<TWorkerItem> item);

	public ReadOnlyGlobalPendingResults<TPostProcessResult> Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<TWorkerItem> items)
	{
		using (CreateProfilerSectionAroundScheduling(scheduler.SchedulingContext, scheduler.WorkIsDoneOnDifferentThread))
		{
			GlobalPendingResults<TPostProcessResult> pendingResults = new GlobalPendingResults<TPostProcessResult>();
			if (Skip(scheduler.SchedulingContext))
			{
				pendingResults.SetResults(CreateEmptyResult());
				return new ReadOnlyGlobalPendingResults<TPostProcessResult>(pendingResults);
			}
			scheduler.EnqueueItemsAndContinueWithResults(scheduler.QueuingContext, Chunk(scheduler.SchedulingContext, items), WorkerWrapper, PostProcessWrapper, pendingResults);
			return new ReadOnlyGlobalPendingResults<TPostProcessResult>(pendingResults);
		}
	}

	protected abstract TPostProcessResult CreateEmptyResult();

	protected abstract ReadOnlyCollection<ReadOnlyCollection<TWorkerItem>> Chunk(GlobalSchedulingContext context, ReadOnlyCollection<TWorkerItem> items);

	private void PostProcessWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<ReadOnlyCollection<TWorkerItem>, TWorkerResult>>, GlobalPendingResults<TPostProcessResult>> workerData)
	{
		using (workerData.Context.TinyProfiler.Section(PostProcessingSectionName))
		{
			TPostProcessResult result = PostProcess(workerData.Context, workerData.Item);
			workerData.Tag.SetResults(result);
		}
	}

	protected abstract TPostProcessResult PostProcess(TWorkerContext context, ReadOnlyCollection<ResultData<ReadOnlyCollection<TWorkerItem>, TWorkerResult>> data);

	private TWorkerResult WorkerWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<TWorkerItem>, GlobalPendingResults<TPostProcessResult>> workerData)
	{
		using (CreateProfilerSectionForProcessItem(workerData.Context, workerData.Item))
		{
			return ProcessItem(workerData.Context, workerData.Item);
		}
	}
}
