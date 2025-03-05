using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.Ordering;

namespace Unity.IL2CPP.Contexts.Components;

public class ReversePInvokeWrapperComponent : ItemsWithMetadataIndexCollector<MethodReference, IReversePInvokeWrapperCollectorResults, IReversePInvokeWrapperCollector, ReversePInvokeWrapperComponent>, IReversePInvokeWrapperCollector
{
	private class NotAvailable : IReversePInvokeWrapperCollector
	{
		public void AddReversePInvokeWrapper(MethodReference method)
		{
			throw new NotSupportedException();
		}
	}

	private class Results : MetadataIndexTableResults<MethodReference>, IReversePInvokeWrapperCollectorResults, IMetadataIndexTableResults<MethodReference>, ITableResults<MethodReference, uint>
	{
		public Results(ReadOnlyCollection<MethodReference> sortedItems, ReadOnlyDictionary<MethodReference, uint> table, ReadOnlyCollection<KeyValuePair<MethodReference, uint>> sortedTable)
			: base(sortedItems, table, sortedTable)
		{
		}
	}

	public void AddReversePInvokeWrapper(MethodReference method)
	{
		AddInternal(method);
	}

	protected override ReversePInvokeWrapperComponent CreateEmptyInstance()
	{
		return new ReversePInvokeWrapperComponent();
	}

	protected override ReversePInvokeWrapperComponent ThisAsFull()
	{
		return this;
	}

	protected override IReversePInvokeWrapperCollector GetNotAvailableWrite()
	{
		return new NotAvailable();
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out IReversePInvokeWrapperCollector writer, out object reader, out ReversePInvokeWrapperComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out IReversePInvokeWrapperCollector writer, out object reader, out ReversePInvokeWrapperComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out IReversePInvokeWrapperCollector writer, out object reader, out ReversePInvokeWrapperComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out IReversePInvokeWrapperCollector writer, out object reader, out ReversePInvokeWrapperComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override ReadOnlyCollection<MethodReference> SortItems(IEnumerable<MethodReference> items)
	{
		return items.ToSortedCollection();
	}

	protected override IReversePInvokeWrapperCollectorResults CreateResultObject(ReadOnlyCollection<MethodReference> sortedItems, ReadOnlyDictionary<MethodReference, uint> table, ReadOnlyCollection<KeyValuePair<MethodReference, uint>> sortedTable)
	{
		return new Results(sortedItems, table, sortedTable);
	}
}
