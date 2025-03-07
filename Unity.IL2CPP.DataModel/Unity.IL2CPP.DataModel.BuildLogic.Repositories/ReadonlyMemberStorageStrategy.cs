using System;
using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

internal class ReadonlyMemberStorageStrategy : IMemberStorageStrategy, IDisposable
{
	public bool IsReadOnly => true;

	public TDataModel GetOrAdd<TArg, TKey, TDataModel>(Dictionary<TKey, TDataModel> mapping, TArg arg, TKey elementType, IMemberStoreCreateCallbacks<TArg, TKey, TDataModel> callbacks)
	{
		if (!mapping.TryGetValue(elementType, out var dataModel))
		{
			ThrowFailedLookup(elementType);
		}
		return dataModel;
	}

	public TDataModel GetOrAdd<TKey, TDataModel>(Dictionary<TKey, TDataModel> mapping, TKey elementType, IMemberStoreCreateCallbacks<TKey, TDataModel> callbacks)
	{
		if (!mapping.TryGetValue(elementType, out var dataModel))
		{
			ThrowFailedLookup(elementType);
		}
		return dataModel;
	}

	private static void ThrowFailedLookup<TKey>(TKey elementType)
	{
		throw new InvalidOperationException($"Attempted to lookup type that hasn't been created. {elementType.GetType().Name}: {elementType}");
	}

	public void Dispose()
	{
	}
}
