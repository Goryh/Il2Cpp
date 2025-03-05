using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.Contexts.Scheduling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class SimpleScheduledStep<TWorkerContext> : ScheduledStep where TWorkerContext : ITinyProfilerProvider
{
	public void Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler)
	{
		if (!Skip(scheduler.SchedulingContext))
		{
			scheduler.Enqueue<TWorkerContext, object>(scheduler.QueuingContext, WorkerWrapper, null);
		}
	}

	private void WorkerWrapper(WorkItemData<TWorkerContext, object> workerData)
	{
		using (workerData.Context.TinyProfiler.Section(Name))
		{
			Worker(workerData.Context);
		}
	}

	protected abstract void Worker(TWorkerContext context);
}
