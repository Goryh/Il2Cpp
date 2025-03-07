namespace Unity.IL2CPP.DataModel;

public interface IMarshalInfoProvider
{
	MarshalInfo MarshalInfo { get; }

	bool HasMarshalInfo { get; }
}
