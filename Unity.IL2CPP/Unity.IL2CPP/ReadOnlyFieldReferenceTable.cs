using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP;

public class ReadOnlyFieldReferenceTable
{
	public readonly ReadOnlyCollection<KeyValuePair<Il2CppRuntimeFieldReference, uint>> Items;

	public ReadOnlyFieldReferenceTable(ReadOnlyCollection<KeyValuePair<Il2CppRuntimeFieldReference, uint>> items)
	{
		Items = items;
	}
}
