namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

internal interface IMemberStoreCreateCallbacks<TArg, TKey, TDataModel>
{
	TDataModel Create(TArg arg, TKey key);

	void OnCreate(TArg arg, TDataModel created);
}
internal interface IMemberStoreCreateCallbacks<TKey, TDataModel>
{
	TDataModel Create(TKey key);

	void OnCreate(TDataModel created);
}
