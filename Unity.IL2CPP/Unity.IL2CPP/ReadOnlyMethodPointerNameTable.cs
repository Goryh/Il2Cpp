using System.Collections.ObjectModel;

namespace Unity.IL2CPP;

public class ReadOnlyMethodPointerNameTable
{
	public readonly ReadOnlyCollection<ReadOnlyMethodPointerNameEntryWithIndex> Items;

	public ReadOnlyMethodPointerNameTable(ReadOnlyCollection<ReadOnlyMethodPointerNameEntryWithIndex> items)
	{
		Items = items;
	}
}
