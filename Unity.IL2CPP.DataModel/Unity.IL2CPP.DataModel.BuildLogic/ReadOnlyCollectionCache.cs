using System;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP.DataModel.BuildLogic;

public static class ReadOnlyCollectionCache<T>
{
	public static readonly ReadOnlyCollection<T> Empty = new ReadOnlyCollection<T>(Array.Empty<T>());
}
