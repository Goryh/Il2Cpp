using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP;

public class ReadOnlyGenericInstanceTable
{
	public readonly ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType[], uint>> SortedItems;

	public readonly ReadOnlyDictionary<IIl2CppRuntimeType[], uint> Table;

	public ReadOnlyGenericInstanceTable(ReadOnlyDictionary<IIl2CppRuntimeType[], uint> table, ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType[], uint>> sortedItems)
	{
		Table = table;
		SortedItems = sortedItems;
	}
}
