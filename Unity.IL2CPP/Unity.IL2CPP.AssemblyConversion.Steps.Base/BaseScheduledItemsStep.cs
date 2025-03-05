using Bee.Core;
using Unity.IL2CPP.Contexts.Providers;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class BaseScheduledItemsStep<TWorkerContext, TWorkerItem> : ScheduledStep where TWorkerContext : ITinyProfilerProvider
{
	protected abstract string ProfilerDetailsForItem(TWorkerItem workerItem);

	protected SectionDisposable CreateProfilerSectionForProcessItem(TWorkerContext context, TWorkerItem item)
	{
		return context.TinyProfiler.Section(Name, ProfilerDetailsForItem(item));
	}
}
