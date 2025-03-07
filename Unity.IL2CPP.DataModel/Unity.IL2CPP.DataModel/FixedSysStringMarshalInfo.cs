namespace Unity.IL2CPP.DataModel;

public sealed class FixedSysStringMarshalInfo : MarshalInfo
{
	public int Size { get; }

	public FixedSysStringMarshalInfo(int size)
		: base(NativeType.FixedSysString)
	{
		Size = size;
	}
}
