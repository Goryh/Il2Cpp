using System;
using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

internal class UnderConstructionMemberStorageStrategy : IMemberStorageStrategy, IDisposable
{
	public bool IsReadOnly => false;

	public TDataModel GetOrAdd<TArg, TKey, TDataModel>(Dictionary<TKey, TDataModel> mapping, TArg arg, TKey elementType, IMemberStoreCreateCallbacks<TArg, TKey, TDataModel> callbacks)
	{
		if (!mapping.TryGetValue(elementType, out var dataModel))
		{
			dataModel = callbacks.Create(arg, elementType);
			mapping.Add(elementType, dataModel);
			callbacks.OnCreate(arg, dataModel);
		}
		return dataModel;
	}

	public TDataModel GetOrAdd<TKey, TDataModel>(Dictionary<TKey, TDataModel> mapping, TKey elementType, IMemberStoreCreateCallbacks<TKey, TDataModel> callbacks)
	{
		if (!mapping.TryGetValue(elementType, out var dataModel))
		{
			dataModel = callbacks.Create(elementType);
			mapping.Add(elementType, dataModel);
			callbacks.OnCreate(dataModel);
		}
		return dataModel;
	}

	public void Dispose()
	{
	}
}
