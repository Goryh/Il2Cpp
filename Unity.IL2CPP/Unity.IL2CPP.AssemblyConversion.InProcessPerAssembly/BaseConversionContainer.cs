using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.InProcessPerAssembly;

public abstract class BaseConversionContainer
{
	public abstract string Name { get; }

	public abstract string CleanName { get; }

	public int Index { get; }

	protected BaseConversionContainer(int index)
	{
		Index = index;
	}

	public abstract bool IncludeTypeDefinitionInContext(TypeReference type);

	public void RunPrimaryCollection(GlobalFullyForkedContext context, GenericSharingAnalysisResults genericSharingAnalysisResults)
	{
		using (context.Services.TinyProfiler.Section("PrimaryCollectionPhase", Name))
		{
			AssemblyConversionResults.PrimaryCollectionPhase results = PrimaryCollectionPhase(context, genericSharingAnalysisResults);
			context.Results.SetPrimaryCollectionResults(results);
		}
	}

	public void RunPrimaryWrite(GlobalFullyForkedContext context)
	{
		using (context.Services.TinyProfiler.Section("PrimaryWritePhase", Name))
		{
			AssemblyConversionResults.PrimaryWritePhase results = PrimaryWritePhase(context);
			context.Results.SetPrimaryWritePhaseResults(results);
		}
	}

	public void RunSecondaryCollection(GlobalFullyForkedContext context)
	{
		using (context.Services.TinyProfiler.Section("SecondaryCollectionPhase", Name))
		{
			AssemblyConversionResults.SecondaryCollectionPhase results = SecondaryCollectionPhase(context);
			context.Results.SetSecondaryCollectionPhaseResults(results);
		}
	}

	public void RunSecondaryWrite(GlobalFullyForkedContext context)
	{
		using (context.Services.TinyProfiler.Section("SecondaryWritePhase", Name))
		{
			AssemblyConversionResults.SecondaryWritePhasePart1 resultsPart1 = SecondaryWritePhasePart1(context);
			context.Results.SetSecondaryWritePhasePart1Results(resultsPart1);
			AssemblyConversionResults.SecondaryWritePhasePart3 resultsPart3 = SecondaryWritePhasePart3(context);
			context.Results.SetSecondaryWritePhasePart3Results(resultsPart3);
			AssemblyConversionResults.SecondaryWritePhase results = SecondaryWritePhasePart4(context);
			context.Results.SetSecondaryWritePhaseResults(results);
		}
	}

	public void RunCompletion(GlobalFullyForkedContext context)
	{
		using (context.Services.TinyProfiler.Section("CompletionPhase", Name))
		{
			CompletionPhase(context);
		}
	}

	protected abstract AssemblyConversionResults.PrimaryCollectionPhase PrimaryCollectionPhase(GlobalFullyForkedContext context, GenericSharingAnalysisResults genericSharingAnalysisResults);

	protected abstract AssemblyConversionResults.PrimaryWritePhase PrimaryWritePhase(GlobalFullyForkedContext context);

	protected abstract AssemblyConversionResults.SecondaryCollectionPhase SecondaryCollectionPhase(GlobalFullyForkedContext context);

	protected abstract AssemblyConversionResults.SecondaryWritePhasePart1 SecondaryWritePhasePart1(GlobalFullyForkedContext context);

	protected abstract AssemblyConversionResults.SecondaryWritePhasePart3 SecondaryWritePhasePart3(GlobalFullyForkedContext context);

	protected abstract AssemblyConversionResults.SecondaryWritePhase SecondaryWritePhasePart4(GlobalFullyForkedContext context);

	protected abstract void CompletionPhase(GlobalFullyForkedContext context);

	protected IPhaseWorkScheduler<TContext> CreateHackedScheduler<TContext>(TContext context, GlobalSchedulingContext schedulingContext)
	{
		return new PhaseWorkSchedulerNoThreading<TContext>(context, schedulingContext);
	}
}
