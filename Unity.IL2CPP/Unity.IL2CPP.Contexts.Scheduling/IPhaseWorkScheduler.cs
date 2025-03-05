using System;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Scheduling;

public interface IPhaseWorkScheduler<TContext> : IWorkScheduler, IDisposable
{
	TContext ContextForMainThread { get; }

	GlobalSchedulingContext SchedulingContext { get; }

	TContext QueuingContext { get; }

	bool WorkIsDoneOnDifferentThread { get; }

	void Wait();

	void WaitForEmptyQueue();
}
