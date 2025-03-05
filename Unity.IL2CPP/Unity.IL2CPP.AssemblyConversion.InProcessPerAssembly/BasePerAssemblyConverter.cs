using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.InProcessPerAssembly;

public abstract class BasePerAssemblyConverter : BaseAssemblyConverter
{
	protected ForkedContextScope<BaseConversionContainer, GlobalFullyForkedContext> Fork(AssemblyConversionContext context)
	{
		ReadOnlyCollection<BaseConversionContainer> containers = CreateContainers(context);
		return Fork(context, containers, CreateContainerOverrideObjects(containers));
	}

	protected abstract ForkedContextScope<BaseConversionContainer, GlobalFullyForkedContext> Fork(AssemblyConversionContext context, ReadOnlyCollection<BaseConversionContainer> containers, ReadOnlyCollection<OverrideObjects> overrideObjects);

	protected abstract ReadOnlyCollection<OverrideObjects> CreateContainerOverrideObjects(ReadOnlyCollection<BaseConversionContainer> containers);

	protected static GenericSharingAnalysisResults RunGenericSharingAnalysis(AssemblyConversionContext context)
	{
		ReadOnlyGlobalPendingResults<GenericSharingAnalysisResults> genericSharingAnalysis;
		using (PhaseWorkSchedulerNoThreading<GlobalPrimaryCollectionContext> scheduler = new PhaseWorkSchedulerNoThreading<GlobalPrimaryCollectionContext>(context.GlobalPrimaryCollectionContext, context.GlobalSchedulingContext))
		{
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterSharedGenerics"))
			{
				genericSharingAnalysis = new GenericSharingAnalysis().Schedule(scheduler, context.Results.Initialize.AllAssembliesOrderedByCostToProcess);
			}
		}
		return genericSharingAnalysis.Result;
	}

	private static ReadOnlyCollection<BaseConversionContainer> CreateContainers(AssemblyConversionContext context)
	{
		ReadOnlyCollection<AssemblyDefinition> assemblies = context.Results.Initialize.AllAssembliesOrderedByCostToProcess;
		AssemblyDefinition entryAssembly = context.Results.Initialize.EntryAssembly;
		List<BaseConversionContainer> containers = new List<BaseConversionContainer>();
		int containerIndex = 0;
		containers.Add(new GenericsConversionContainer(assemblies, containerIndex++));
		foreach (AssemblyDefinition asm in assemblies)
		{
			string name = PathFactoryComponent.GenerateFileNamePrefixForAssembly(asm);
			string cleanName = asm.CleanFileName;
			containers.Add(new AssemblyConversionContainer(asm, asm == entryAssembly, name, cleanName, containerIndex++));
		}
		return containers.AsReadOnly();
	}

	protected static IPhaseWorkScheduler<TContext> CreateHackedScheduler<TContext>(TContext context, GlobalSchedulingContext schedulingContext)
	{
		return new PhaseWorkSchedulerNoThreading<TContext>(context, schedulingContext);
	}
}
