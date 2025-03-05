using System;
using Unity.IL2CPP.Contexts.Forking.Providers;

namespace Unity.IL2CPP.Contexts.Forking.Steps;

public class PrimaryCollectionDataForker : BaseDataForker<GlobalPrimaryCollectionContext>
{
	private GlobalPrimaryCollectionContext _forkedGlobalCollectionContext;

	private GlobalMinimalContext _forkedGlobalMinimalContext;

	private GlobalReadOnlyContext _forkedGlobalReadOnlyContext;

	public PrimaryCollectionDataForker(IUnrestrictedContextDataProvider context)
		: base(context)
	{
	}

	public override GlobalPrimaryCollectionContext CreateForkedContext()
	{
		_forkedGlobalReadOnlyContext = new GlobalReadOnlyContext(_forkedProvider);
		_forkedGlobalMinimalContext = new GlobalMinimalContext(_forkedProvider, _forkedGlobalReadOnlyContext);
		_forkedGlobalCollectionContext = new GlobalPrimaryCollectionContext(_forkedProvider, _forkedProvider, _forkedGlobalMinimalContext, _forkedGlobalReadOnlyContext);
		return _forkedGlobalCollectionContext;
	}

	protected override ReadWrite<TWrite, TRead, TFull> PickFork<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
	{
		return component.ForkForPrimaryCollection;
	}

	protected override WriteOnly<TWrite, TFull> PickFork<TWrite, TFull>(IForkableComponent<TWrite, object, TFull> component)
	{
		return component.ForkForPrimaryCollection;
	}

	protected override ReadOnly<TRead, TFull> PickFork<TRead, TFull>(IForkableComponent<object, TRead, TFull> component)
	{
		return component.ForkForPrimaryCollection;
	}

	protected override Action<TFull> PickMerge<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
	{
		return component.MergeForPrimaryCollection;
	}
}
