using System.Collections.ObjectModel;
using System.Linq;
using Bee.Core;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class ScheduledTwoInItemsStepFuncWithContinueFunc<TWorkerContext, TWorkerItem, TWorkerItem2, TWorkerResult, TContinueResult> : BaseScheduledItemsStep<TWorkerContext, TWorkerItem> where TWorkerContext : ITinyProfilerProvider
{
	protected abstract string PostProcessingSectionName { get; }

	protected abstract TWorkerResult ProcessItem(TWorkerContext context, TWorkerItem item);

	protected abstract TWorkerResult ProcessItem(TWorkerContext context, TWorkerItem2 item);

	protected abstract string ProfilerDetailsForItem2(TWorkerItem2 workerItem);

	protected SectionDisposable CreateProfilerSectionForProcessItem2(TWorkerContext context, TWorkerItem2 item)
	{
		return context.TinyProfiler.Section(Name, ProfilerDetailsForItem2(item));
	}

	public ReadOnlyGlobalPendingResults<TContinueResult> Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<TWorkerItem> items, TWorkerItem2 item2)
	{
		return Schedule(scheduler, items, new TWorkerItem2[1] { item2 }.AsReadOnly());
	}

	public ReadOnlyGlobalPendingResults<TContinueResult> Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<TWorkerItem> items, ReadOnlyCollection<TWorkerItem2> items2)
	{
		using (CreateProfilerSectionAroundScheduling(scheduler.SchedulingContext, scheduler.WorkIsDoneOnDifferentThread))
		{
			GlobalPendingResults<TContinueResult> pendingResults = new GlobalPendingResults<TContinueResult>();
			if (Skip(scheduler.SchedulingContext))
			{
				pendingResults.SetResults(CreateEmptyResult());
				return new ReadOnlyGlobalPendingResults<TContinueResult>(pendingResults);
			}
			scheduler.EnqueueItemsAndContinueWithResults(scheduler.QueuingContext, OrderItemsForScheduling(scheduler.SchedulingContext, items, items2), WorkerWrapper, PostProcessWrapper, pendingResults);
			return new ReadOnlyGlobalPendingResults<TContinueResult>(pendingResults);
		}
	}

	protected abstract TContinueResult CreateEmptyResult();

	protected abstract ReadOnlyCollection<object> OrderItemsForScheduling(GlobalSchedulingContext context, ReadOnlyCollection<TWorkerItem> items, ReadOnlyCollection<TWorkerItem2> items2);

	private void PostProcessWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<object, TWorkerResult>>, GlobalPendingResults<TContinueResult>> workerData)
	{
		using (workerData.Context.TinyProfiler.Section(PostProcessingSectionName))
		{
			TContinueResult result = PostProcess(workerData.Context, workerData.Item.Select((ResultData<object, TWorkerResult> r) => r.Result).ToList().AsReadOnly());
			workerData.Tag.SetResults(result);
		}
	}

	protected abstract TContinueResult PostProcess(TWorkerContext context, ReadOnlyCollection<TWorkerResult> data);

	private TWorkerResult WorkerWrapper(WorkItemData<TWorkerContext, object, GlobalPendingResults<TContinueResult>> workerData)
	{
		if (workerData.Item is TWorkerItem item1)
		{
			using (CreateProfilerSectionForProcessItem(workerData.Context, item1))
			{
				return ProcessItem(workerData.Context, item1);
			}
		}
		TWorkerItem2 item2 = (TWorkerItem2)workerData.Item;
		using (CreateProfilerSectionForProcessItem2(workerData.Context, item2))
		{
			return ProcessItem(workerData.Context, item2);
		}
	}
}
