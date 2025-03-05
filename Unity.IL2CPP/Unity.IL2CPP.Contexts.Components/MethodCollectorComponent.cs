using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.Ordering;

namespace Unity.IL2CPP.Contexts.Components;

public class MethodCollectorComponent : ItemsWithMetadataIndexCollectorPhaseSortSupport<MethodReference, IMethodCollectorResults, IMethodCollector, MethodCollectorComponent>, IMethodCollector
{
	private class Results : MetadataIndexTableResults<MethodReference>, IMethodCollectorResults, IMetadataIndexTableResults<MethodReference>, ITableResults<MethodReference, uint>
	{
		public Results(ReadOnlyCollection<MethodReference> sortedItems, ReadOnlyDictionary<MethodReference, uint> table, ReadOnlyCollection<KeyValuePair<MethodReference, uint>> sortedTable)
			: base(sortedItems, table, sortedTable)
		{
		}
	}

	private class Comparer : IComparer<MethodReference>
	{
		public int Compare(MethodReference x, MethodReference y)
		{
			return x.Compare(y);
		}
	}

	private class NotAvailable : ItemsWithMetadataIndexCollectorNotAvailable<MethodReference>, IMethodCollector, IMethodCollectorResults, IMetadataIndexTableResults<MethodReference>, ITableResults<MethodReference, uint>
	{
	}

	public MethodCollectorComponent()
		: base(isForkedInstance: false)
	{
	}

	private MethodCollectorComponent(bool isForkedInstance)
		: base(isForkedInstance)
	{
	}

	public void PhaseSortItemsToReduceFinalSortTime()
	{
		PhaseSortItems();
	}

	public void Add(MethodReference method)
	{
		AddInternal(method);
	}

	protected override List<MethodReference> SortItems(List<MethodReference> items)
	{
		items.Sort(new Comparer());
		return items;
	}

	protected override IMethodCollectorResults CreateResultObject(ReadOnlyCollection<MethodReference> sortedItems, ReadOnlyDictionary<MethodReference, uint> table, ReadOnlyCollection<KeyValuePair<MethodReference, uint>> sortedTable)
	{
		return new Results(sortedItems, table, sortedTable);
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out IMethodCollector writer, out object reader, out MethodCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out IMethodCollector writer, out object reader, out MethodCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out IMethodCollector writer, out object reader, out MethodCollectorComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out IMethodCollector writer, out object reader, out MethodCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override MethodCollectorComponent CreateEmptyInstance()
	{
		return new MethodCollectorComponent(isForkedInstance: true);
	}

	protected override MethodCollectorComponent ThisAsFull()
	{
		return this;
	}

	protected override IMethodCollector GetNotAvailableWrite()
	{
		return new NotAvailable();
	}
}
