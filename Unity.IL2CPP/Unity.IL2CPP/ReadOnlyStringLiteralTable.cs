using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP;

public class ReadOnlyStringLiteralTable
{
	public readonly ReadOnlyCollection<KeyValuePair<string, uint>> Items;

	public ReadOnlyStringLiteralTable(ReadOnlyCollection<KeyValuePair<string, uint>> items)
	{
		Items = items;
	}
}
