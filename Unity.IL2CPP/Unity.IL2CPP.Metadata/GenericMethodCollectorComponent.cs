using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.Ordering;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata;

public class GenericMethodCollectorComponent : ItemsWithMetadataIndexCollectorPhaseSortSupport<Il2CppMethodSpec, IGenericMethodCollectorResults, IGenericMethodCollector, GenericMethodCollectorComponent>, IGenericMethodCollector
{
	private class Results : MetadataIndexTableResults<Il2CppMethodSpec>, IGenericMethodCollectorResults, IMetadataIndexTableResults<Il2CppMethodSpec>, ITableResults<Il2CppMethodSpec, uint>
	{
		public Results(ReadOnlyCollection<Il2CppMethodSpec> sortedItems, ReadOnlyDictionary<Il2CppMethodSpec, uint> table, ReadOnlyCollection<KeyValuePair<Il2CppMethodSpec, uint>> sortedTable)
			: base(sortedItems, table, sortedTable)
		{
		}

		public uint GetIndex(MethodReference method)
		{
			return GetIndex(new Il2CppMethodSpec(method));
		}

		public bool HasIndex(MethodReference method)
		{
			return HasIndex(new Il2CppMethodSpec(method));
		}

		public bool TryGetValue(MethodReference method, out uint genericMethodIndex)
		{
			return TryGetValue(new Il2CppMethodSpec(method), out genericMethodIndex);
		}
	}

	private class Comparer : IComparer<Il2CppMethodSpec>
	{
		public int Compare(Il2CppMethodSpec x, Il2CppMethodSpec y)
		{
			return x.GenericMethod.Compare(y.GenericMethod);
		}
	}

	private class NotAvailable : ItemsWithMetadataIndexCollectorNotAvailable<Il2CppMethodSpec>, IGenericMethodCollector, IGenericMethodCollectorResults, IMetadataIndexTableResults<Il2CppMethodSpec>, ITableResults<Il2CppMethodSpec, uint>
	{
		public uint GetIndex(MethodReference method)
		{
			throw new NotImplementedException();
		}

		public bool HasIndex(MethodReference method)
		{
			throw new NotImplementedException();
		}

		public bool TryGetValue(MethodReference method, out uint genericMethodIndex)
		{
			throw new NotImplementedException();
		}

		public void Add(SourceWritingContext context, MethodReference method)
		{
			throw new NotImplementedException();
		}

		public void Add(PrimaryCollectionContext context, MethodReference method)
		{
			throw new NotImplementedException();
		}

		public void Add(SecondaryCollectionContext context, MethodReference method)
		{
			throw new NotImplementedException();
		}
	}

	public GenericMethodCollectorComponent()
		: base(isForkedInstance: false, (IEqualityComparer<Il2CppMethodSpec>)new Il2CppMethodSpecEqualityComparer())
	{
	}

	private GenericMethodCollectorComponent(bool isForkedInstance)
		: base(isForkedInstance, (IEqualityComparer<Il2CppMethodSpec>)new Il2CppMethodSpecEqualityComparer())
	{
	}

	public void PhaseSortItemsToReduceFinalSortTime()
	{
		PhaseSortItems();
	}

	public void Add(SourceWritingContext context, MethodReference method)
	{
		Add(context.Global.Collectors.Types, method);
	}

	public void Add(PrimaryCollectionContext context, MethodReference method)
	{
		Add(context.Global.Collectors.Types, method);
	}

	private void Add(ITypeCollector typeCollector, MethodReference method)
	{
		Il2CppMethodSpec methodRef = new Il2CppMethodSpec(method);
		if (ContainsInternal(methodRef))
		{
			return;
		}
		IIl2CppRuntimeType[] genericInstanceData = null;
		IIl2CppRuntimeType[] methodGenericInstanceData = null;
		if (method.DeclaringType.IsGenericInstance)
		{
			genericInstanceData = ((GenericInstanceType)method.DeclaringType).GenericArguments.Select((TypeReference g) => typeCollector.Add(g)).ToArray();
		}
		if (method.IsGenericInstance)
		{
			methodGenericInstanceData = ((GenericInstanceMethod)method).GenericArguments.Select((TypeReference g) => typeCollector.Add(g)).ToArray();
		}
		AddInternal(new Il2CppMethodSpec(method, methodGenericInstanceData, genericInstanceData));
	}

	protected override GenericMethodCollectorComponent CreateEmptyInstance()
	{
		return new GenericMethodCollectorComponent(isForkedInstance: true);
	}

	protected override GenericMethodCollectorComponent ThisAsFull()
	{
		return this;
	}

	protected override IGenericMethodCollector GetNotAvailableWrite()
	{
		return new NotAvailable();
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out IGenericMethodCollector writer, out object reader, out GenericMethodCollectorComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out IGenericMethodCollector writer, out object reader, out GenericMethodCollectorComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out IGenericMethodCollector writer, out object reader, out GenericMethodCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out IGenericMethodCollector writer, out object reader, out GenericMethodCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override List<Il2CppMethodSpec> SortItems(List<Il2CppMethodSpec> items)
	{
		items.Sort(new Comparer());
		return items;
	}

	protected override IGenericMethodCollectorResults CreateResultObject(ReadOnlyCollection<Il2CppMethodSpec> sortedItems, ReadOnlyDictionary<Il2CppMethodSpec, uint> table, ReadOnlyCollection<KeyValuePair<Il2CppMethodSpec, uint>> sortedTable)
	{
		return new Results(sortedItems, table, sortedTable);
	}
}
