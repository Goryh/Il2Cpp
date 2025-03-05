using System;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Scheduling;

public class PhaseWorkSchedulerNoThreading<TContext> : ImmediateSchedulerComponent, IPhaseWorkScheduler<TContext>, IWorkScheduler, IDisposable
{
	private readonly TContext _context;

	private readonly GlobalSchedulingContext _schedulingContext;

	public TContext ContextForMainThread => _context;

	public GlobalSchedulingContext SchedulingContext => _schedulingContext;

	public TContext QueuingContext => ContextForMainThread;

	public bool WorkIsDoneOnDifferentThread => false;

	public PhaseWorkSchedulerNoThreading(TContext context, GlobalSchedulingContext schedulingContext)
	{
		_context = context;
		_schedulingContext = schedulingContext;
	}

	public void Dispose()
	{
	}

	public void Wait()
	{
	}

	public void WaitForEmptyQueue()
	{
	}
}
