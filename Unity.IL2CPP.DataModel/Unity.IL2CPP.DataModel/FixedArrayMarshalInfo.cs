namespace Unity.IL2CPP.DataModel;

public sealed class FixedArrayMarshalInfo : MarshalInfo
{
	public NativeType ElementType { get; }

	public int Size { get; }

	public FixedArrayMarshalInfo(NativeType elementType, int size)
		: base(NativeType.FixedArray)
	{
		ElementType = elementType;
		Size = size;
	}
}
