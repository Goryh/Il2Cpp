using Unity.IL2CPP.Contexts.Scheduling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class CustomScheduledStep<TWorkerContext> : ScheduledStep
{
	public void Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler)
	{
		using (CreateProfilerSectionAroundScheduling(scheduler.SchedulingContext, scheduler.WorkIsDoneOnDifferentThread))
		{
			if (!Skip(scheduler.SchedulingContext))
			{
				DoScheduling(scheduler.QueuingContext);
			}
		}
	}

	protected abstract void DoScheduling(TWorkerContext context);
}
