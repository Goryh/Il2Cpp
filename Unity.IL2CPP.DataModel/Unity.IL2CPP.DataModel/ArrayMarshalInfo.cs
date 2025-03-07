namespace Unity.IL2CPP.DataModel;

public sealed class ArrayMarshalInfo : MarshalInfo
{
	public NativeType ElementType { get; }

	public int SizeParameterIndex { get; }

	public int Size { get; }

	public int SizeParameterMultiplier { get; }

	public ArrayMarshalInfo(NativeType elementType, int sizeParameterIndex, int size, int sizeParameterMultiplier)
		: base(NativeType.Array)
	{
		ElementType = elementType;
		SizeParameterIndex = sizeParameterIndex;
		Size = size;
		SizeParameterMultiplier = sizeParameterMultiplier;
	}
}
