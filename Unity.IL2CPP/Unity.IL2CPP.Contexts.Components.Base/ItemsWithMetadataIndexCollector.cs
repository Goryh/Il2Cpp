using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP.Contexts.Components.Base;

public abstract class ItemsWithMetadataIndexCollector<TItem, TComplete, TWrite, TFull> : ForkAndMergeHashSetCollectorBase<TItem, TComplete, TWrite, TFull> where TComplete : IMetadataIndexTableResults<TItem> where TFull : ForkAndMergeHashSetCollectorBase<TItem, TComplete, TWrite, TFull>, TWrite
{
	private readonly IEqualityComparer<TItem> _comparer;

	public ItemsWithMetadataIndexCollector(IEqualityComparer<TItem> comparer)
		: base(comparer)
	{
		_comparer = comparer;
	}

	protected ItemsWithMetadataIndexCollector()
		: base((IEqualityComparer<TItem>)null)
	{
	}

	protected override TComplete BuildResults(ReadOnlyCollection<TItem> sortedItem)
	{
		Dictionary<TItem, uint> table = ((_comparer == null) ? new Dictionary<TItem, uint>() : new Dictionary<TItem, uint>(_comparer));
		List<KeyValuePair<TItem, uint>> sortedTableItems = new List<KeyValuePair<TItem, uint>>();
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
