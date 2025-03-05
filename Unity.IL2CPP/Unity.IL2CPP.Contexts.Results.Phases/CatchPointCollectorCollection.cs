using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Debugger;

namespace Unity.IL2CPP.Contexts.Results.Phases;

public class CatchPointCollectorCollection
{
	private class NotCollected : ICatchPointProvider
	{
		private readonly AssemblyDefinition _assembly;

		private string ExceptionMessage => $"Catch Points were never collected for assembly : {_assembly.Name}";

		public int NumCatchPoints
		{
			get
			{
				throw new NotSupportedException(ExceptionMessage);
			}
		}

		public IEnumerable<CatchPointInfo> AllCatchPoints
		{
			get
			{
				throw new NotSupportedException(ExceptionMessage);
			}
		}

		public NotCollected(AssemblyDefinition assembly)
		{
			_assembly = assembly;
		}
	}

	private readonly ReadOnlyDictionary<AssemblyDefinition, ICatchPointProvider> _providers;

	public static CatchPointCollectorCollection Empty => new CatchPointCollectorCollection(new Dictionary<AssemblyDefinition, ICatchPointProvider>().AsReadOnly());

	public CatchPointCollectorCollection(ReadOnlyDictionary<AssemblyDefinition, ICatchPointProvider> providers)
	{
		_providers = providers;
	}

	public ICatchPointProvider GetCollector(AssemblyDefinition assembly)
	{
		if (_providers.TryGetValue(assembly, out var provider))
		{
			return provider;
		}
		return new NotCollected(assembly);
	}

	public static CatchPointCollectorCollection Merge(IEnumerable<CatchPointCollectorCollection> results)
	{
		Dictionary<AssemblyDefinition, ICatchPointProvider> merged = new Dictionary<AssemblyDefinition, ICatchPointProvider>();
		foreach (CatchPointCollectorCollection result in results)
		{
			foreach (KeyValuePair<AssemblyDefinition, ICatchPointProvider> item in result._providers)
			{
				merged.Add(item.Key, item.Value);
			}
		}
		return new CatchPointCollectorCollection(merged.AsReadOnly());
	}
}
