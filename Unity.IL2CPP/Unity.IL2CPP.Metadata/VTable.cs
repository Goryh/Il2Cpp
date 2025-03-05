using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Metadata;

public class VTable
{
	private readonly ReadOnlyCollection<VTableSlot> _slots;

	private readonly Dictionary<TypeReference, int> _interfaceOffsets;

	public ReadOnlyCollection<VTableSlot> Slots => _slots;

	public Dictionary<TypeReference, int> InterfaceOffsets => _interfaceOffsets;

	public VTable(ReadOnlyCollection<VTableSlot> slots, Dictionary<TypeReference, int> interfaceOffsets)
	{
		_slots = slots;
		_interfaceOffsets = interfaceOffsets;
	}
}
