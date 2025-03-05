using System;
using Unity.IL2CPP.Contexts.Forking;

namespace Unity.IL2CPP.Contexts.Components.Base;

public abstract class StatelessComponentBase<TWrite, TRead, TFull> : ComponentBase<TWrite, TRead, TFull> where TFull : ComponentBase<TWrite, TRead, TFull>, TWrite, TRead
{
	protected void WriteOnlyFork(in ForkingData data, out TWrite writer, out TRead reader, out TFull full)
	{
		base.WriteOnlyFork(in data, out writer, out reader, out full, ForkMode.ReuseThis, MergeMode.None);
	}

	protected override TFull CreateEmptyInstance()
	{
		throw new NotSupportedException();
	}

	protected override TFull CreateCopyInstance()
	{
		throw new NotSupportedException();
	}

	protected override void HandleMergeForAdd(TFull forked)
	{
		throw new NotSupportedException($"A stateless component should never use {1} because there should never have been anything added to merge");
	}

	protected override void HandleMergeForMergeValues(TFull forked)
	{
		throw new NotSupportedException($"A stateless component should never use {2} because there should never have been anything added to merge");
	}

	protected override void ForkForPartialPerAssembly(in ForkingData data, out TWrite writer, out TRead reader, out TFull full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForFullPerAssembly(in ForkingData data, out TWrite writer, out TRead reader, out TFull full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}
}
