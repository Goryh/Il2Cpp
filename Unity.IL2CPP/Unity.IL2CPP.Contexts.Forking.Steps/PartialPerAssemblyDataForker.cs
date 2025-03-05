using System;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts.Forking.Providers;

namespace Unity.IL2CPP.Contexts.Forking.Steps;

public class PartialPerAssemblyDataForker : BaseDataForker<GlobalFullyForkedContext>
{
	private GlobalWriteContext _forkedGlobalWriteContext;

	private GlobalMinimalContext _forkedGlobalMinimalContext;

	private GlobalReadOnlyContext _forkedGlobalReadOnlyContext;

	private GlobalPrimaryCollectionContext _forkedGlobalPrimaryCollectionContext;

	private GlobalSecondaryCollectionContext _forkedGlobalSecondaryCollectionContext;

	private GlobalSchedulingContext _forkedGlobalSchedulingContext;

	private readonly AssemblyConversionResults _phaseResultsContainer;

	public PartialPerAssemblyDataForker(IUnrestrictedContextDataProvider context)
		: this(context, SetupForkedConversionResultsWithExistingResults(context))
	{
	}

	private PartialPerAssemblyDataForker(IUnrestrictedContextDataProvider context, AssemblyConversionResults phaseResultsContainer)
		: base((ForkedDataProvider)new PerAssemblyForkedDataProvider(context, new ForkedDataContainer(), phaseResultsContainer))
	{
		_phaseResultsContainer = phaseResultsContainer;
	}

	private static AssemblyConversionResults SetupForkedConversionResultsWithExistingResults(IUnrestrictedContextDataProvider context)
	{
		AssemblyConversionResults assemblyConversionResults = new AssemblyConversionResults();
		assemblyConversionResults.SetInitializePhaseResults(new AssemblyConversionResults.InitializePhase(null, null, null, context.PhaseResults.Initialize.GenericLimits));
		assemblyConversionResults.SetSetupPhaseResults(context.PhaseResults.Setup);
		return assemblyConversionResults;
	}

	public override GlobalFullyForkedContext CreateForkedContext()
	{
		_forkedGlobalReadOnlyContext = new GlobalReadOnlyContext(_forkedProvider);
		_forkedGlobalMinimalContext = new GlobalMinimalContext(_forkedProvider, _forkedGlobalReadOnlyContext);
		_forkedGlobalWriteContext = new GlobalWriteContext(_forkedProvider, _forkedProvider, _forkedGlobalMinimalContext, _forkedGlobalReadOnlyContext);
		_forkedGlobalPrimaryCollectionContext = new GlobalPrimaryCollectionContext(_forkedProvider, _forkedProvider, _forkedGlobalMinimalContext, _forkedGlobalReadOnlyContext);
		_forkedGlobalSecondaryCollectionContext = new GlobalSecondaryCollectionContext(_forkedProvider, _forkedProvider, _forkedGlobalMinimalContext, _forkedGlobalReadOnlyContext);
		_forkedGlobalSchedulingContext = new GlobalSchedulingContext(_forkedProvider);
		return new GlobalFullyForkedContext(_forkedGlobalReadOnlyContext, _forkedGlobalMinimalContext, _forkedGlobalPrimaryCollectionContext, _forkedGlobalWriteContext, _forkedGlobalSecondaryCollectionContext, _forkedGlobalSchedulingContext, _phaseResultsContainer, _container);
	}

	protected override ReadWrite<TWrite, TRead, TFull> PickFork<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
	{
		return component.ForkForPartialPerAssembly;
	}

	protected override WriteOnly<TWrite, TFull> PickFork<TWrite, TFull>(IForkableComponent<TWrite, object, TFull> component)
	{
		return component.ForkForPartialPerAssembly;
	}

	protected override ReadOnly<TRead, TFull> PickFork<TRead, TFull>(IForkableComponent<object, TRead, TFull> component)
	{
		return component.ForkForPartialPerAssembly;
	}

	protected override Action<TFull> PickMerge<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
	{
		return component.MergeForPartialPerAssembly;
	}
}
