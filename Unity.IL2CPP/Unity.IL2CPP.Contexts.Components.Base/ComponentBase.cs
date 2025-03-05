using System;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Forking.Steps;

namespace Unity.IL2CPP.Contexts.Components.Base;

public abstract class ComponentBase<TWrite, TRead, TFull> : IForkableComponent<TWrite, TRead, TFull> where TFull : ComponentBase<TWrite, TRead, TFull>, TWrite, TRead
{
	public enum ForkMode
	{
		Empty,
		Copy,
		ReuseThis,
		Pooled
	}

	public enum MergeMode
	{
		None,
		Add,
		MergeValues
	}

	private MergeMode _mergeMode;

	private PoolContainer<TFull> _pool;

	protected ComponentBase()
	{
		_mergeMode = MergeMode.None;
	}

	protected virtual void ReadOnlyFork(in ForkingData data, out TWrite writer, out TRead reader, out TFull full, ForkMode forkMode, MergeMode mergeMode = MergeMode.None)
	{
		ReadOnlyForkInternal(in data, out writer, out reader, out full, forkMode, mergeMode);
	}

	protected virtual void ReadOnlyFork(in ForkingData data, out TWrite writer, out TRead reader, out TFull full, ForkMode forkMode = ForkMode.ReuseThis)
	{
		ReadOnlyForkInternal(in data, out writer, out reader, out full, forkMode, MergeMode.None);
	}

	protected virtual void ReadOnlyForkWithMergeAbility(in ForkingData data, out TWrite writer, out TRead reader, out TFull full, ForkMode forkMode = ForkMode.ReuseThis, MergeMode mergeMode = MergeMode.None)
	{
		ReadOnlyForkInternal(in data, out writer, out reader, out full, forkMode, mergeMode);
	}

	private void ReadOnlyForkInternal(in ForkingData data, out TWrite writer, out TRead reader, out TFull full, ForkMode forkMode, MergeMode mergeMode)
	{
		writer = ((typeof(TWrite) == typeof(object)) ? default(TWrite) : GetNotAvailableWrite());
		reader = (TRead)(full = SetupForkedInstance(in data, forkMode, mergeMode));
	}

	protected virtual void WriteOnlyFork(in ForkingData data, out TWrite writer, out TRead reader, out TFull full, ForkMode forkMode = ForkMode.Empty, MergeMode mergeMode = MergeMode.Add)
	{
		writer = (TWrite)(full = SetupForkedInstance(in data, forkMode, mergeMode));
		reader = ((typeof(TRead) == typeof(object)) ? default(TRead) : GetNotAvailableRead());
	}

	protected virtual void NotAvailableFork(in ForkingData data, out TWrite writer, out TRead reader, out TFull full, ForkMode forkMode = ForkMode.Empty)
	{
		full = SetupForkedInstance(in data, forkMode, MergeMode.None);
		writer = ((typeof(TWrite) == typeof(object)) ? default(TWrite) : GetNotAvailableWrite());
		reader = ((typeof(TRead) == typeof(object)) ? default(TRead) : GetNotAvailableRead());
	}

	protected virtual void ReadWriteFork(in ForkingData data, out TWrite writer, out TRead reader, out TFull full, ForkMode mode = ForkMode.Copy, MergeMode mergeMode = MergeMode.Add)
	{
		full = SetupForkedInstance(in data, mode, mergeMode);
		writer = (TWrite)full;
		reader = (TRead)full;
	}

	private TFull SetupForkedInstance(in ForkingData data, ForkMode forkMode, MergeMode mergeMode)
	{
		TFull instance = forkMode switch
		{
			ForkMode.Empty => CreateEmptyInstance(), 
			ForkMode.Copy => CreateCopyInstance(), 
			ForkMode.ReuseThis => ThisAsFull(), 
			ForkMode.Pooled => GetPooledInstance(in data), 
			_ => throw new ArgumentException($"Unhandled {"ForkMode"} value of `{forkMode}`"), 
		};
		if (forkMode == ForkMode.ReuseThis && _mergeMode != mergeMode)
		{
			throw new ArgumentException($"Cannot use {2} when the merge mode ({mergeMode}) differs from the parent({_mergeMode}).  This can cause issues tracking the merge state when nested forking occurs because we overwrite the parents merge mode.");
		}
		instance._mergeMode = mergeMode;
		return instance;
	}

	protected void Merge(TFull forked)
	{
		try
		{
			switch (forked._mergeMode)
			{
			case MergeMode.None:
				break;
			case MergeMode.Add:
				HandleMergeForAdd(forked);
				break;
			case MergeMode.MergeValues:
				HandleMergeForMergeValues(forked);
				break;
			default:
				throw new ArgumentException($"Unhandled merge mode {forked._mergeMode}");
			}
		}
		catch (Exception innerException)
		{
			throw new InvalidOperationException($"Exception while merging {GetType()}", innerException);
		}
	}

	protected abstract void HandleMergeForAdd(TFull forked);

	protected abstract void HandleMergeForMergeValues(TFull forked);

	private TFull GetPooledInstance(in ForkingData data)
	{
		if (_pool == null)
		{
			_pool = new PoolContainer<TFull>(data.Count);
		}
		TFull instance = _pool.Items[data.Index];
		if (instance == null)
		{
			instance = (_pool.Items[data.Index] = CreatePooledInstance());
		}
		else
		{
			instance.ResetPooledInstanceStateIfNecessary();
			SyncPooledInstanceWithParent(ThisAsFull());
		}
		return instance;
	}

	protected abstract void ResetPooledInstanceStateIfNecessary();

	protected abstract void SyncPooledInstanceWithParent(TFull parent);

	protected abstract TFull CreateEmptyInstance();

	protected abstract TFull CreateCopyInstance();

	protected abstract TFull CreatePooledInstance();

	protected abstract TFull ThisAsFull();

	protected abstract TRead ThisAsRead();

	protected abstract TWrite GetNotAvailableWrite();

	protected abstract TRead GetNotAvailableRead();

	protected abstract void ForkForPrimaryWrite(in ForkingData data, out TWrite writer, out TRead reader, out TFull full);

	protected abstract void ForkForPrimaryCollection(in ForkingData data, out TWrite writer, out TRead reader, out TFull full);

	protected abstract void ForkForSecondaryWrite(in ForkingData data, out TWrite writer, out TRead reader, out TFull full);

	protected abstract void ForkForSecondaryCollection(in ForkingData data, out TWrite writer, out TRead reader, out TFull full);

	protected virtual void ForkForPartialPerAssembly(in ForkingData data, out TWrite writer, out TRead reader, out TFull full)
	{
		ReadWriteFork(in data, out writer, out reader, out full, ForkMode.Empty);
	}

	protected virtual void ForkForFullPerAssembly(in ForkingData data, out TWrite writer, out TRead reader, out TFull full)
	{
		ReadWriteFork(in data, out writer, out reader, out full, ForkMode.Empty, MergeMode.None);
	}

	void IForkableComponent<TWrite, TRead, TFull>.ForkForPrimaryWrite(in ForkingData data, out TWrite writer, out TRead reader, out TFull full)
	{
		ForkForPrimaryWrite(in data, out writer, out reader, out full);
	}

	void IForkableComponent<TWrite, TRead, TFull>.MergeForPrimaryWrite(TFull forked)
	{
		Merge(forked);
	}

	void IForkableComponent<TWrite, TRead, TFull>.ForkForPrimaryCollection(in ForkingData data, out TWrite writer, out TRead reader, out TFull full)
	{
		ForkForPrimaryCollection(in data, out writer, out reader, out full);
	}

	void IForkableComponent<TWrite, TRead, TFull>.MergeForPrimaryCollection(TFull forked)
	{
		Merge(forked);
	}

	void IForkableComponent<TWrite, TRead, TFull>.ForkForSecondaryWrite(in ForkingData data, out TWrite writer, out TRead reader, out TFull full)
	{
		ForkForSecondaryWrite(in data, out writer, out reader, out full);
	}

	void IForkableComponent<TWrite, TRead, TFull>.MergeForSecondaryWrite(TFull forked)
	{
		Merge(forked);
	}

	void IForkableComponent<TWrite, TRead, TFull>.ForkForSecondaryCollection(in ForkingData data, out TWrite writer, out TRead reader, out TFull full)
	{
		ForkForSecondaryCollection(in data, out writer, out reader, out full);
	}

	void IForkableComponent<TWrite, TRead, TFull>.MergeForSecondaryCollection(TFull forked)
	{
		Merge(forked);
	}

	void IForkableComponent<TWrite, TRead, TFull>.ForkForPartialPerAssembly(in ForkingData data, out TWrite writer, out TRead reader, out TFull full)
	{
		ForkForPartialPerAssembly(in data, out writer, out reader, out full);
	}

	void IForkableComponent<TWrite, TRead, TFull>.MergeForPartialPerAssembly(TFull forked)
	{
		Merge(forked);
	}

	void IForkableComponent<TWrite, TRead, TFull>.ForkForFullPerAssembly(in ForkingData data, out TWrite writer, out TRead reader, out TFull full)
	{
		ForkForFullPerAssembly(in data, out writer, out reader, out full);
	}

	void IForkableComponent<TWrite, TRead, TFull>.MergeForFullPerAssembly(TFull forked)
	{
		Merge(forked);
	}
}
