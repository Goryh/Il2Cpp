using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

public class BlittableStructMarshalInfoWriter : DefaultMarshalInfoWriter
{
	private readonly TypeReference _type;

	private readonly MarshalType _marshalType;

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return new MarshaledType[1]
		{
			new MarshaledType(_type.CppNameForVariable)
		};
	}

	public override int GetNativeSizeWithoutPointers(ReadOnlyContext context)
	{
		return MarshalingUtils.GetNativeSizeWithoutPointers(context, _type, _marshalType);
	}

	public BlittableStructMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalType marshalType)
		: base(type)
	{
		_type = type;
		_marshalType = marshalType;
	}
}
