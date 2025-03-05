using System;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class ScheduledStep : BaseStep<GlobalSchedulingContext>
{
	protected IDisposable CreateProfilerSectionAroundScheduling(GlobalSchedulingContext context, bool parallelEnabled)
	{
		if (parallelEnabled)
		{
			return new DisabledSection();
		}
		return context.Services.TinyProfiler.Section(StepUtilities.FormatAllStepName(Name));
	}
}
