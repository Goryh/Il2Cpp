using System.Runtime.InteropServices;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Attributes;

[StructLayout(LayoutKind.Explicit)]
public readonly struct AttributeDataItem
{
	[FieldOffset(0)]
	public readonly byte[] Data;

	[FieldOffset(0)]
	public readonly IIl2CppRuntimeType TypeData;

	[FieldOffset(0)]
	public readonly AttributeDataItem[] DataArray;

	[FieldOffset(0)]
	public readonly CustomAttributeArgumentData[] NestedDataArray;

	[FieldOffset(8)]
	public readonly AttributeDataItemType Type;

	public AttributeDataItem(byte[] data)
	{
		if (data == null)
		{
			Type = AttributeDataItemType.Null;
		}
		else
		{
			Type = AttributeDataItemType.BinaryData;
		}
		TypeData = null;
		DataArray = null;
		NestedDataArray = null;
		Data = data;
	}

	public AttributeDataItem(IIl2CppRuntimeType typeData)
	{
		Type = AttributeDataItemType.Type;
		Data = null;
		DataArray = null;
		NestedDataArray = null;
		TypeData = typeData;
	}

	public AttributeDataItem(AttributeDataItemType type, AttributeDataItem[] array)
	{
		Type = type;
		Data = null;
		TypeData = null;
		NestedDataArray = null;
		DataArray = array;
	}

	public AttributeDataItem(CustomAttributeArgumentData[] array)
	{
		Type = AttributeDataItemType.ObjectArray;
		Data = null;
		TypeData = null;
		DataArray = null;
		NestedDataArray = array;
	}
}
