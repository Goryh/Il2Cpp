namespace Unity.IL2CPP.DataModel;

public class MarshalInfo
{
	public NativeType NativeType { get; }

	public MarshalInfo(NativeType native)
	{
		NativeType = native;
	}
}
