using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Metadata;

public readonly struct VTableSlot
{
	public readonly MethodReference Method;

	public readonly VTableSlotAttr Attr;

	public VTableSlot(MethodReference method)
		: this(method, VTableSlotAttr.Normal)
	{
	}

	public VTableSlot(MethodReference method, VTableSlotAttr attr)
	{
		Method = method;
		Attr = attr;
	}

	public static implicit operator VTableSlot(MethodReference method)
	{
		return new VTableSlot(method);
	}

	public override string ToString()
	{
		if (Attr == VTableSlotAttr.Normal)
		{
			return Method?.FullName;
		}
		return $"{Method?.FullName ?? ""} - {Attr}";
	}
}
