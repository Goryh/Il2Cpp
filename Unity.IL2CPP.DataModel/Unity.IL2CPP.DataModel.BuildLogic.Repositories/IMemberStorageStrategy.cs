using System;
using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

internal interface IMemberStorageStrategy : IDisposable
{
	bool IsReadOnly { get; }

	TDataModel GetOrAdd<TArg, TKey, TDataModel>(Dictionary<TKey, TDataModel> mapping, TArg arg, TKey elementType, IMemberStoreCreateCallbacks<TArg, TKey, TDataModel> callbacks);

	TDataModel GetOrAdd<TKey, TDataModel>(Dictionary<TKey, TDataModel> mapping, TKey elementType, IMemberStoreCreateCallbacks<TKey, TDataModel> callbacks);
}
