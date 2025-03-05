using System;
using Unity.IL2CPP.Contexts.Forking.Providers;

namespace Unity.IL2CPP.Contexts.Forking.Steps;

public class SecondaryWriteDataForker : BaseDataForker<GlobalWriteContext>
{
	private GlobalWriteContext _forkedGlobalWriteContext;

	private GlobalMinimalContext _forkedGlobalMinimalContext;

	private GlobalReadOnlyContext _forkedGlobalReadOnlyContext;

	public SecondaryWriteDataForker(IUnrestrictedContextDataProvider context)
		: base(context)
	{
	}

	public override GlobalWriteContext CreateForkedContext()
	{
		_forkedGlobalReadOnlyContext = new GlobalReadOnlyContext(_forkedProvider);
		_forkedGlobalMinimalContext = new GlobalMinimalContext(_forkedProvider, _forkedGlobalReadOnlyContext);
		_forkedGlobalWriteContext = new GlobalWriteContext(_forkedProvider, _forkedProvider, _forkedGlobalMinimalContext, _forkedGlobalReadOnlyContext);
		return _forkedGlobalWriteContext;
	}

	protected override ReadWrite<TWrite, TRead, TFull> PickFork<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
	{
		return component.ForkForSecondaryWrite;
	}

	protected override WriteOnly<TWrite, TFull> PickFork<TWrite, TFull>(IForkableComponent<TWrite, object, TFull> component)
	{
		return component.ForkForSecondaryWrite;
	}

	protected override ReadOnly<TRead, TFull> PickFork<TRead, TFull>(IForkableComponent<object, TRead, TFull> component)
	{
		return component.ForkForSecondaryWrite;
	}

	protected override Action<TFull> PickMerge<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
	{
		return component.MergeForSecondaryWrite;
	}
}
