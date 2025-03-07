namespace Unity.IL2CPP.DataModel.Modify.Definitions;

internal interface IMarshalInfoUpdater : IMarshalInfoProvider
{
	bool MarshalInfoHasBeenUpdated { get; }

	void UpdateMarshalInfo(MarshalInfo marshalInfo);
}
