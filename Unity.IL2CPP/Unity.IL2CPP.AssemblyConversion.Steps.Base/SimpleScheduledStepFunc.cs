using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.Contexts.Scheduling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class SimpleScheduledStepFunc<TWorkerContext, TResult> : ScheduledStep where TWorkerContext : ITinyProfilerProvider
{
	public ReadOnlyGlobalPendingResults<TResult> Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler)
	{
		GlobalPendingResults<TResult> pendingResults = new GlobalPendingResults<TResult>();
		if (Skip(scheduler.SchedulingContext))
		{
			pendingResults.SetResults(CreateEmptyResult());
			return new ReadOnlyGlobalPendingResults<TResult>(pendingResults);
		}
		scheduler.Enqueue(scheduler.QueuingContext, WorkerWrapper, pendingResults);
		return new ReadOnlyGlobalPendingResults<TResult>(pendingResults);
	}

	protected abstract TResult CreateEmptyResult();

	private void WorkerWrapper(WorkItemData<TWorkerContext, GlobalPendingResults<TResult>> workerData)
	{
		using (workerData.Context.TinyProfiler.Section(Name))
		{
			TResult result = Worker(workerData.Context);
			workerData.Tag.SetResults(result);
		}
	}

	protected abstract TResult Worker(TWorkerContext context);
}
