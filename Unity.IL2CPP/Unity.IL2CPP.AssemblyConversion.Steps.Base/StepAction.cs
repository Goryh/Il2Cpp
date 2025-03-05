using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base;

public abstract class StepAction<TContext> : BaseStep<TContext> where TContext : class
{
	public void Run(TContext context, ITinyProfilerService tinyProfiler)
	{
		using (tinyProfiler.Section(Name))
		{
			if (!Skip(context))
			{
				Process(context);
			}
		}
	}

	protected abstract void Process(TContext context);
}
