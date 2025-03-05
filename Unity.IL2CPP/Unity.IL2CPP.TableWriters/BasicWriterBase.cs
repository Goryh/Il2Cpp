using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;

namespace Unity.IL2CPP.TableWriters;

public abstract class BasicWriterBase
{
	public void Schedule(SourceWritingContext context)
	{
		context.Global.Services.Scheduler.Enqueue<GlobalWriteContext, object>(context.Global, Worker, null);
	}

	public void Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		scheduler.Enqueue<GlobalWriteContext, object>(scheduler.QueuingContext, Worker, null);
	}

	private void Worker(WorkItemData<GlobalWriteContext, object> data)
	{
		WriteFile(data.Context.CreateSourceWritingContext());
	}

	protected abstract void WriteFile(SourceWritingContext context);
}
