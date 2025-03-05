using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class GlobalSimpleScheduledStepFunc<TWorkerContext, TResult> : ScheduledStep where TWorkerContext : ITinyProfilerProvider
{
	public ReadOnlyGlobalPendingResults<TResult> Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<AssemblyDefinition> items)
	{
		GlobalPendingResults<TResult> pendingResults = new GlobalPendingResults<TResult>();
		if (Skip(scheduler.SchedulingContext))
		{
			pendingResults.SetResults(CreateEmptyResult());
			return new ReadOnlyGlobalPendingResults<TResult>(pendingResults);
		}
		scheduler.Enqueue(scheduler.QueuingContext, WorkerWrapper, (pendingResults, items));
		return new ReadOnlyGlobalPendingResults<TResult>(pendingResults);
	}

	protected abstract TResult CreateEmptyResult();

	private void WorkerWrapper(WorkItemData<TWorkerContext, (GlobalPendingResults<TResult>, ReadOnlyCollection<AssemblyDefinition>)> workerData)
	{
		using (workerData.Context.TinyProfiler.Section(Name))
		{
			TResult result = Worker(workerData.Context, workerData.Tag.Item2);
			workerData.Tag.Item1.SetResults(result);
		}
	}

	protected abstract TResult Worker(TWorkerContext context, ReadOnlyCollection<AssemblyDefinition> assemblies);
}
