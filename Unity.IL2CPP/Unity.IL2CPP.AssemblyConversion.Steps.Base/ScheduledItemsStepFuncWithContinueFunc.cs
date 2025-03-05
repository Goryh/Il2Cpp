using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class ScheduledItemsStepFuncWithContinueFunc<TWorkerContext, TWorkerItem, TWorkerResult, TContinueResult> : BaseScheduledItemsStep<TWorkerContext, TWorkerItem> where TWorkerContext : ITinyProfilerProvider
{
	protected abstract string PostProcessingSectionName { get; }

	protected abstract TWorkerResult ProcessItem(TWorkerContext context, TWorkerItem item);

	public ReadOnlyGlobalPendingResults<TContinueResult> Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<TWorkerItem> items)
	{
		using (CreateProfilerSectionAroundScheduling(scheduler.SchedulingContext, scheduler.WorkIsDoneOnDifferentThread))
		{
			GlobalPendingResults<TContinueResult> pendingResults = new GlobalPendingResults<TContinueResult>();
			if (Skip(scheduler.SchedulingContext))
			{
				pendingResults.SetResults(CreateEmptyResult());
				return new ReadOnlyGlobalPendingResults<TContinueResult>(pendingResults);
			}
			scheduler.EnqueueItemsAndContinueWithResults(scheduler.QueuingContext, items, WorkerWrapper, PostProcessWrapper, pendingResults);
			return new ReadOnlyGlobalPendingResults<TContinueResult>(pendingResults);
		}
	}

	protected abstract TContinueResult CreateEmptyResult();

	private void PostProcessWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<TWorkerItem, TWorkerResult>>, GlobalPendingResults<TContinueResult>> workerData)
	{
		using (workerData.Context.TinyProfiler.Section(PostProcessingSectionName))
		{
			TContinueResult result = PostProcess(workerData.Context, workerData.Item);
			workerData.Tag.SetResults(result);
		}
	}

	protected abstract TContinueResult PostProcess(TWorkerContext context, ReadOnlyCollection<ResultData<TWorkerItem, TWorkerResult>> data);

	private TWorkerResult WorkerWrapper(WorkItemData<TWorkerContext, TWorkerItem, GlobalPendingResults<TContinueResult>> workerData)
	{
		using (CreateProfilerSectionForProcessItem(workerData.Context, workerData.Item))
		{
			return ProcessItem(workerData.Context, workerData.Item);
		}
	}
}
