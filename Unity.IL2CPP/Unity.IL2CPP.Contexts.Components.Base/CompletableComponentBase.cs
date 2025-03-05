using System;
using Unity.IL2CPP.Contexts.Forking;

namespace Unity.IL2CPP.Contexts.Components.Base;

public abstract class CompletableComponentBase<TComplete, TWrite, TFull> : ComponentBase<TWrite, object, TFull> where TFull : CompletableComponentBase<TComplete, TWrite, TFull>, TWrite
{
	private bool _complete;

	public virtual TComplete Complete()
	{
		AssertNotComplete();
		_complete = true;
		return GetResults();
	}

	protected abstract TComplete GetResults();

	protected void SetComplete()
	{
		_complete = true;
	}

	protected override object ThisAsRead()
	{
		throw new NotSupportedException();
	}

	protected override object GetNotAvailableRead()
	{
		throw new NotSupportedException();
	}

	protected override void ReadWriteFork(in ForkingData data, out TWrite writer, out object reader, out TFull full, ForkMode mode = ForkMode.Copy, MergeMode mergeMode = MergeMode.Add)
	{
		throw new NotSupportedException("Completable components have no read interface");
	}

	protected override void WriteOnlyFork(in ForkingData data, out TWrite writer, out object reader, out TFull full, ForkMode forkMode = ForkMode.Empty, MergeMode mergeMode = MergeMode.Add)
	{
		AssertNotComplete();
		base.WriteOnlyFork(in data, out writer, out reader, out full, forkMode, mergeMode);
	}

	protected override void ReadOnlyFork(in ForkingData data, out TWrite writer, out object reader, out TFull full, ForkMode forkMode = ForkMode.ReuseThis)
	{
		throw new NotSupportedException("Completable components have no read interface");
	}

	protected override void ReadOnlyForkWithMergeAbility(in ForkingData data, out TWrite writer, out object reader, out TFull full, ForkMode forkMode = ForkMode.ReuseThis, MergeMode mergeMode = MergeMode.None)
	{
		throw new NotSupportedException("Completable components have no read interface");
	}

	protected override void ForkForPartialPerAssembly(in ForkingData data, out TWrite writer, out object reader, out TFull full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForFullPerAssembly(in ForkingData data, out TWrite writer, out object reader, out TFull full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full, ForkMode.Empty, MergeMode.None);
	}

	protected void AssertNotComplete()
	{
		if (_complete)
		{
			throw new InvalidOperationException("Once Complete() has been called, items cannot be added");
		}
	}
}
