using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Forking;

public class ForkedContextScope<TItem, TContext> : IDisposable
{
	public struct Data
	{
		public readonly TItem Value;

		public readonly int Index;

		private readonly Func<int, TContext> _onDemandContext;

		private TContext _context;

		public TContext Context
		{
			get
			{
				if (_context == null)
				{
					InitializeContext();
				}
				return _context;
			}
		}

		public Data(TItem value, TContext context, int index)
		{
			Value = value;
			_context = context;
			Index = index;
			_onDemandContext = null;
		}

		public Data(TItem value, Func<int, TContext> onDemandContext, int index)
		{
			Value = value;
			_context = default(TContext);
			Index = index;
			_onDemandContext = onDemandContext;
		}

		public void InitializeContext()
		{
			if (_onDemandContext != null)
			{
				_context = _onDemandContext(Index);
			}
		}
	}

	private readonly Dictionary<object, Action[]> _mergeBack = new Dictionary<object, Action[]>();

	private readonly Dictionary<object, Func<IDataForker<TContext>, ForkingData, Action>> _forkDataProviderSetupTable = new Dictionary<object, Func<IDataForker<TContext>, ForkingData, Action>>();

	private readonly List<IDataForker<TContext>> _forkDataProviders = new List<IDataForker<TContext>>();

	private readonly Dictionary<TItem, TContext> _forkedContexts = new Dictionary<TItem, TContext>();

	private readonly int _count;

	private readonly int _maxContextCount;

	private readonly ReadOnlyCollection<TItem> _items;

	private readonly bool _useParallelMerge;

	private readonly ForkCreationMode _creationMode;

	private readonly IPhaseResultsSetter<TContext> _phaseResultsSetter;

	private readonly ITinyProfilerService _tinyProfiler;

	public ReadOnlyCollection<Data> Items
	{
		get
		{
			List<Data> collection = new List<Data>();
			foreach (TItem item in _items)
			{
				if (_forkedContexts[item] != null)
				{
					collection.Add(new Data(item, _forkedContexts[item], collection.Count));
				}
				else
				{
					collection.Add(new Data(item, OnDemandContextAccess, collection.Count));
				}
			}
			return collection.AsReadOnly();
		}
	}

	private ForkedContextScope(ITinyProfilerService tinyProfiler, ReadOnlyCollection<TItem> items, bool useParallelMerge, ForkCreationMode creationMode, int maxContextCount)
	{
		_tinyProfiler = tinyProfiler;
		_items = items;
		_count = items.Count;
		_maxContextCount = ((maxContextCount > 0) ? maxContextCount : items.Count);
		_useParallelMerge = useParallelMerge;
		_creationMode = creationMode;
	}

	public ForkedContextScope(IUnrestrictedContextDataProvider context, ReadOnlyCollection<TItem> items, Func<IUnrestrictedContextDataProvider, IDataForker<TContext>> providerFactory, OverrideObjects overrideObjects, bool useParallelMerge = true, ForkCreationMode creationMode = ForkCreationMode.OnDemand, int maxContextCount = -1)
		: this((ITinyProfilerService)context.Services.TinyProfiler, items, useParallelMerge, creationMode, maxContextCount)
	{
		Setup(context, providerFactory, overrideObjects, items.Count);
	}

	public ForkedContextScope(IUnrestrictedContextDataProvider context, ReadOnlyCollection<TItem> items, Func<IUnrestrictedContextDataProvider, IDataForker<TContext>> providerFactory, bool useParallelMerge = true, ForkCreationMode creationMode = ForkCreationMode.OnDemand, int maxContextCount = -1)
		: this((ITinyProfilerService)context.Services.TinyProfiler, items, useParallelMerge, creationMode, maxContextCount)
	{
		Setup(context, providerFactory, null);
	}

	public ForkedContextScope(IUnrestrictedContextDataProvider context, ReadOnlyCollection<TItem> items, Func<IUnrestrictedContextDataProvider, IDataForker<TContext>> providerFactory, ReadOnlyCollection<OverrideObjects> overrideObjects, bool useParallelMerge = true, ForkCreationMode creationMode = ForkCreationMode.OnDemand, IPhaseResultsSetter<TContext> phaseResultsSetter = null, int maxContextCount = -1)
		: this((ITinyProfilerService)context.Services.TinyProfiler, items, useParallelMerge, creationMode, maxContextCount)
	{
		_phaseResultsSetter = phaseResultsSetter;
		Setup(context, providerFactory, overrideObjects);
	}

	private void Setup(IUnrestrictedContextDataProvider context, Func<IUnrestrictedContextDataProvider, IDataForker<TContext>> providerFactory, OverrideObjects overrideObject, int count)
	{
		List<OverrideObjects> sharedOverrides = new List<OverrideObjects>();
		for (int i = 0; i < count; i++)
		{
			sharedOverrides.Add(overrideObject);
		}
		Setup(context, providerFactory, sharedOverrides.AsReadOnly());
	}

	private void Setup(IUnrestrictedContextDataProvider context, Func<IUnrestrictedContextDataProvider, IDataForker<TContext>> providerFactory, ReadOnlyCollection<OverrideObjects> overrideObjects)
	{
		using (_tinyProfiler.Section("ForkedContextScope.Setup"))
		{
			CreateForkedDataProviders(context, providerFactory);
			SetupMergeEntries(context, overrideObjects);
			PopulateProvidersWithAllForks();
			PopulateContextTable();
		}
	}

	private void SetupMergeEntries(IUnrestrictedContextDataProvider context, ReadOnlyCollection<OverrideObjects> overrideObjects)
	{
		ForkingRegistration.SetupMergeEntries<TContext>(context, RegisterCollector, overrideObjects);
	}

	private void RegisterCollector(object collector, Func<IDataForker<TContext>, ForkingData, Action> fork)
	{
		_mergeBack[collector] = new Action[_count];
		_forkDataProviderSetupTable[collector] = fork;
	}

	private TContext OnDemandContextAccess(int index)
	{
		TItem contextKeyForIndex = _items[index];
		if (_forkedContexts.TryGetValue(contextKeyForIndex, out var context) && context != null)
		{
			return context;
		}
		using (_tinyProfiler.Section("Create Forked Context"))
		{
			ForkingData forkingData = new ForkingData(index, _maxContextCount);
			foreach (object key in _mergeBack.Keys)
			{
				Action[] mergeBack = _mergeBack[key];
				Func<IDataForker<TContext>, ForkingData, Action> createFork = _forkDataProviderSetupTable[key];
				using (_tinyProfiler.Section("Fork Component", key.GetType().ToString()))
				{
					mergeBack[index] = createFork(_forkDataProviders[index], forkingData);
				}
			}
			context = _forkDataProviders[index].CreateForkedContext();
			_forkedContexts[contextKeyForIndex] = context;
			return context;
		}
	}

	private void CreateForkedDataProviders(IUnrestrictedContextDataProvider context, Func<IUnrestrictedContextDataProvider, IDataForker<TContext>> providerFactory)
	{
		for (int i = 0; i < _count; i++)
		{
			_forkDataProviders.Add(providerFactory(context));
		}
	}

	private void PopulateContextTable()
	{
		if (_creationMode == ForkCreationMode.Upfront)
		{
			for (int i = 0; i < _count; i++)
			{
				_forkedContexts.Add(_items[i], _forkDataProviders[i].CreateForkedContext());
			}
		}
		else
		{
			for (int j = 0; j < _count; j++)
			{
				_forkedContexts.Add(_items[j], default(TContext));
			}
		}
	}

	private void PopulateProvidersWithAllForks()
	{
		if (_creationMode == ForkCreationMode.Upfront)
		{
			Parallel.ForEach(_mergeBack.Keys, PopulateForksInProvidersForObject);
			return;
		}
		foreach (object key in _mergeBack.Keys)
		{
			InitializeForksInProvidersForObject(key);
		}
	}

	private void InitializeForksInProvidersForObject(object obj)
	{
		Action[] mergeBack = _mergeBack[obj];
		for (int i = 0; i < _count; i++)
		{
			mergeBack[i] = null;
		}
	}

	private void PopulateForksInProvidersForObject(object obj)
	{
		Action[] mergeBack = _mergeBack[obj];
		Func<IDataForker<TContext>, ForkingData, Action> createFork = _forkDataProviderSetupTable[obj];
		for (int i = 0; i < _count; i++)
		{
			mergeBack[i] = createFork(_forkDataProviders[i], new ForkingData(i, _maxContextCount));
		}
	}

	private void MergeBack()
	{
		using (_tinyProfiler.Section("ForkedContextScope.MergeBack"))
		{
			if (_creationMode == ForkCreationMode.OnDemand && _forkedContexts.Values.All((TContext v) => v == null))
			{
				return;
			}
			if (_useParallelMerge)
			{
				Parallel.ForEach(_mergeBack, delegate(KeyValuePair<object, Action[]> pair)
				{
					MergeForObject(pair.Value, pair.Key.GetType());
				});
			}
			else
			{
				foreach (KeyValuePair<object, Action[]> pair2 in _mergeBack)
				{
					MergeForObject(pair2.Value, pair2.Key.GetType());
				}
			}
			_phaseResultsSetter?.SetPhaseResults(_forkedContexts.Values.ToList().AsReadOnly());
		}
	}

	private void MergeForObject(Action[] merges, Type componentType)
	{
		using (_tinyProfiler.Section("ForkedContextScope.MergingComponent", componentType.ToString()))
		{
			for (int i = 0; i < merges.Length; i++)
			{
				merges[i]?.Invoke();
			}
		}
	}

	public void Dispose()
	{
		MergeBack();
	}
}
