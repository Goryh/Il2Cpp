using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Diagnostics;

namespace Unity.IL2CPP.Contexts.Components.Base;

public abstract class ItemsWithMetadataIndexCollectorPhaseSortSupport<TItem, TComplete, TWrite, TFull> : CompletableComponentBase<TComplete, TWrite, TFull>, IDumpableState where TComplete : IMetadataIndexTableResults<TItem> where TFull : ItemsWithMetadataIndexCollectorPhaseSortSupport<TItem, TComplete, TWrite, TFull>, TWrite
{
	private readonly bool _isForkedInstance;

	private readonly HashSet<TItem> _items;

	private readonly IEqualityComparer<TItem> _comparer;

	private List<TItem> _phaseSortedItems;

	private readonly List<TItem> _newUnsortedItems = new List<TItem>();

	protected ItemsWithMetadataIndexCollectorPhaseSortSupport(bool isForkedInstance, IEqualityComparer<TItem> comparer)
	{
		_isForkedInstance = isForkedInstance;
		_comparer = comparer;
		_items = ((comparer == null) ? new HashSet<TItem>() : new HashSet<TItem>(comparer));
	}

	protected ItemsWithMetadataIndexCollectorPhaseSortSupport(bool isForkedInstance)
		: this(isForkedInstance, (IEqualityComparer<TItem>)null)
	{
	}

	protected void PhaseSortItems()
	{
		if (_isForkedInstance)
		{
			throw new NotSupportedException("Supporting phase sorting of a forked instance is not implemented.  It would be more complicated and is currently not needed");
		}
		if (_phaseSortedItems == null)
		{
			_phaseSortedItems = SortItems(_items.ToList());
		}
		else if (_newUnsortedItems.Count != 0)
		{
			_phaseSortedItems.AddRange(SortItems(_newUnsortedItems));
			_newUnsortedItems.Clear();
		}
	}

	protected virtual bool AddInternal(TItem item)
	{
		AssertNotComplete();
		bool num = _items.Add(item);
		if (num && _phaseSortedItems != null)
		{
			_newUnsortedItems.Add(item);
		}
		return num;
	}

	protected bool ContainsInternal(TItem item)
	{
		AssertNotComplete();
		return _items.Contains(item);
	}

	protected override TComplete GetResults()
	{
		if (_phaseSortedItems == null)
		{
			return BuildResults(SortItems(_items.ToList()).AsReadOnly());
		}
		PhaseSortItems();
		return BuildResults(_phaseSortedItems.AsReadOnly());
	}

	protected ReadOnlyHashSet<TItem> CompleteForMerge()
	{
		SetComplete();
		return _items.AsReadOnly();
	}

	protected override void HandleMergeForAdd(TFull forked)
	{
		foreach (TItem item in forked.CompleteForMerge())
		{
			AddInternal(item);
		}
	}

	protected override void HandleMergeForMergeValues(TFull forked)
	{
		throw new NotSupportedException();
	}

	protected override void ResetPooledInstanceStateIfNecessary()
	{
		throw new NotImplementedException("It's not clear if there would be a benefit from pooling this type of component");
	}

	protected override void SyncPooledInstanceWithParent(TFull parent)
	{
		throw new NotImplementedException("It's not clear if there would be a benefit from pooling this type of component");
	}

	protected override TFull CreatePooledInstance()
	{
		throw new NotImplementedException("It's not clear if there would be a benefit from pooling this type of component");
	}

	void IDumpableState.DumpState(StringBuilder builder)
	{
		CollectorStateDumper.AppendCollection(builder, "_items", SortItems(_items.ToList()), DumpStateItemToString);
	}

	protected virtual string DumpStateItemToString(TItem item)
	{
		return item.ToString();
	}

	protected abstract List<TItem> SortItems(List<TItem> items);

	protected TComplete BuildResults(ReadOnlyCollection<TItem> sortedItem)
	{
		Dictionary<TItem, uint> table = ((_comparer == null) ? new Dictionary<TItem, uint>(sortedItem.Count) : new Dictionary<TItem, uint>(sortedItem.Count, _comparer));
		List<KeyValuePair<TItem, uint>> sortedTableItems = new List<KeyValuePair<TItem, uint>>(sortedItem.Count);
		uint counter = 0u;
		foreach (TItem item in sortedItem)
		{
			table.Add(item, counter);
			sortedTableItems.Add(new KeyValuePair<TItem, uint>(item, counter));
			counter++;
		}
		return CreateResultObject(sortedItem, table.AsReadOnly(), sortedTableItems.AsReadOnly());
	}

	protected override TFull CreateCopyInstance()
	{
		throw new NotSupportedException("Normally index collectors do not use copy");
	}

	protected abstract TComplete CreateResultObject(ReadOnlyCollection<TItem> sortedItems, ReadOnlyDictionary<TItem, uint> table, ReadOnlyCollection<KeyValuePair<TItem, uint>> sortedTable);
}
