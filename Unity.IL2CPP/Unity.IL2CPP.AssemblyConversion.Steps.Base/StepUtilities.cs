using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public static class StepUtilities
{
	public static string FormatAllStepName(string name)
	{
		return "All " + name;
	}

	public static void EnqueueStep(this IWorkScheduler scheduler, GlobalPrimaryCollectionContext context, StepAction<GlobalPrimaryCollectionContext> action)
	{
		context.Services.Scheduler.Enqueue(context, delegate(WorkItemData<GlobalPrimaryCollectionContext, StepAction<GlobalPrimaryCollectionContext>> data)
		{
			data.Tag.Run(data.Context, data.Context.Services.TinyProfiler);
		}, action);
	}
}
