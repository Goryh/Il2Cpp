using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.Contexts.Scheduling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class ContextFreeScheduledStep<TWorkerContext> : ScheduledStep where TWorkerContext : ITinyProfilerProvider
{
	public void Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler)
	{
		if (!Skip(scheduler.SchedulingContext))
		{
			scheduler.Enqueue(scheduler.QueuingContext, WorkerWrapper);
		}
	}

	private void WorkerWrapper(TWorkerContext workerContext)
	{
		using (workerContext.TinyProfiler.Section(Name))
		{
			Worker();
		}
	}

	protected abstract void Worker();
}
