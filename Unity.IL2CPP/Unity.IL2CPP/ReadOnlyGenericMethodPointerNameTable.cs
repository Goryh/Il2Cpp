using System.Collections.ObjectModel;

namespace Unity.IL2CPP;

public class ReadOnlyGenericMethodPointerNameTable
{
	public readonly ReadOnlyCollection<ReadOnlyMethodPointerNameEntry> Items;

	public ReadOnlyGenericMethodPointerNameTable(ReadOnlyCollection<ReadOnlyMethodPointerNameEntry> items)
	{
		Items = items;
	}
}
