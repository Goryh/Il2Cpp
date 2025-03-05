using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP;

public class ReadOnlyInteropTable
{
	public readonly ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType, InteropData>> Items;

	public ReadOnlyInteropTable(ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType, InteropData>> items)
	{
		Items = items;
	}
}
