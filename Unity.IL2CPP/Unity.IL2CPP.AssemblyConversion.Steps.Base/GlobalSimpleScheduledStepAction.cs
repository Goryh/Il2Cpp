using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Providers;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class GlobalSimpleScheduledStepAction<TWorkerContext> : ScheduledStep where TWorkerContext : ITinyProfilerProvider
{
	public void Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<AssemblyDefinition> items)
	{
		if (!Skip(scheduler.SchedulingContext))
		{
			scheduler.Enqueue(scheduler.QueuingContext, WorkerWrapper, items);
		}
	}

	private void WorkerWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<AssemblyDefinition>> workerData)
	{
		using (workerData.Context.TinyProfiler.Section(Name))
		{
			Worker(workerData.Context, workerData.Tag);
		}
	}

	protected abstract void Worker(TWorkerContext context, ReadOnlyCollection<AssemblyDefinition> assemblies);
}
