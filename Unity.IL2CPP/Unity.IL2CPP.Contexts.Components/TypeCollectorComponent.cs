using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Components;

public class TypeCollectorComponent : CompletableStatefulComponentBase<ITypeCollectorResults, ITypeCollector, TypeCollectorComponent>, ITypeCollector
{
	private class NotAvailable : ITypeCollector, ITypeCollectorResults
	{
		ReadOnlyCollection<IIl2CppRuntimeType> ITypeCollectorResults.SortedItems
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		int ITypeCollectorResults.GetIndex(IIl2CppRuntimeType typeData)
		{
			throw new NotSupportedException();
		}

		IIl2CppRuntimeType ITypeCollector.Add(TypeReference type, int attrs)
		{
			throw new NotSupportedException();
		}
	}

	private class Results : ITypeCollectorResults
	{
		private readonly IReadOnlyDictionary<IIl2CppRuntimeType, int> _itemLookup;

		public ReadOnlyCollection<IIl2CppRuntimeType> SortedItems { get; }

		public Results(ReadOnlyCollection<IIl2CppRuntimeType> sortedItems, IReadOnlyDictionary<IIl2CppRuntimeType, int> itemLookup)
		{
			SortedItems = sortedItems;
			_itemLookup = itemLookup;
		}

		int ITypeCollectorResults.GetIndex(IIl2CppRuntimeType typeData)
		{
			if (_itemLookup.TryGetValue(typeData, out var index))
			{
				return index;
			}
			throw new InvalidOperationException("Il2CppTypeIndexFor type " + typeData.Type.FullName + " does not exist.");
		}
	}

	private readonly Dictionary<Il2CppTypeData, IIl2CppRuntimeType> _data;

	private readonly bool _isForkedInstance;

	private List<IIl2CppRuntimeType> _phaseSortedItems;

	private readonly List<IIl2CppRuntimeType> _newUnsortedItems = new List<IIl2CppRuntimeType>();

	public TypeCollectorComponent()
		: this(isForkedInstance: false)
	{
	}

	private TypeCollectorComponent(bool isForkedInstance)
	{
		_isForkedInstance = isForkedInstance;
		_data = new Dictionary<Il2CppTypeData, IIl2CppRuntimeType>(new Il2CppTypeDataEqualityComparer());
	}

	public void PhaseSortItemsToReduceFinalSortTime()
	{
		if (_isForkedInstance)
		{
			throw new NotSupportedException("Supporting phase sorting of a forked instance is not implemented.  It would be more complicated and is currently not needed");
		}
		if (_phaseSortedItems == null)
		{
			_phaseSortedItems = _data.Values.ToList();
			_phaseSortedItems.Sort(new Il2CppRuntimeTypeComparer());
		}
		else if (_newUnsortedItems.Count != 0)
		{
			_newUnsortedItems.Sort(new Il2CppRuntimeTypeComparer());
			_phaseSortedItems.AddRange(_newUnsortedItems);
			_newUnsortedItems.Clear();
		}
	}

	public IIl2CppRuntimeType Add(TypeReference type, int attrs = 0)
	{
		AssertNotComplete();
		type = type.WithoutModifiers();
		if (type.UnderlyingType() is FunctionPointerType functionPointerType)
		{
			type = functionPointerType.TypeForReflectionUsage;
		}
		Il2CppTypeData data = new Il2CppTypeData(type, attrs);
		if (_data.TryGetValue(data, out var runtimeType))
		{
			return runtimeType;
		}
		if (type.IsGenericInstance)
		{
			GenericInstanceType genericInstanceType = (GenericInstanceType)type;
			runtimeType = new Il2CppGenericInstanceRuntimeType(genericInstanceType, attrs, genericInstanceType.GenericArguments.Select((TypeReference t) => Add(t)).ToArray(), Add(genericInstanceType.Resolve()));
		}
		else if (type.IsArray)
		{
			ArrayType arrayType = (ArrayType)type;
			runtimeType = new Il2CppArrayRuntimeType(arrayType, attrs, Add(arrayType.ElementType));
		}
		else if (type.IsPointer)
		{
			PointerType pointerType = (PointerType)type;
			runtimeType = new Il2CppPtrRuntimeType(pointerType, attrs, Add(pointerType.ElementType));
		}
		else if (type.IsByReference)
		{
			ByReferenceType byReferenceType = (ByReferenceType)type;
			runtimeType = new Il2CppByReferenceRuntimeType(byReferenceType, attrs, Add(byReferenceType.ElementType));
		}
		else
		{
			runtimeType = new Il2CppRuntimeType(type, attrs);
		}
		_data.Add(data, runtimeType);
		return runtimeType;
	}

	public bool WasCollected(TypeReference type, int attrs = 0)
	{
		return _data.ContainsKey(new Il2CppTypeData(type, attrs));
	}

	public IReadOnlyCollection<IIl2CppRuntimeType> GetCollectedItems()
	{
		return _data.Values.ToList().AsReadOnly();
	}

	protected override void DumpState(StringBuilder builder)
	{
		foreach (KeyValuePair<Il2CppTypeData, IIl2CppRuntimeType> item in _data.ItemsSortedByKey())
		{
			builder.AppendLine(item.Key.Type.FullName);
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, builder);
			handler.AppendLiteral("  Attrs = ");
			handler.AppendFormatted(item.Key.Attrs);
			builder.AppendLine(ref handler);
		}
	}

	protected override void HandleMergeForAdd(TypeCollectorComponent forked)
	{
		if (_phaseSortedItems == null)
		{
			foreach (KeyValuePair<Il2CppTypeData, IIl2CppRuntimeType> il2cppTypeData in forked._data)
			{
				_data[il2cppTypeData.Key] = il2cppTypeData.Value;
			}
			return;
		}
		foreach (KeyValuePair<Il2CppTypeData, IIl2CppRuntimeType> il2cppTypeData2 in forked._data)
		{
			if (!_data.ContainsKey(il2cppTypeData2.Key))
			{
				_newUnsortedItems.Add(il2cppTypeData2.Value);
			}
			_data[il2cppTypeData2.Key] = il2cppTypeData2.Value;
		}
	}

	protected override void HandleMergeForMergeValues(TypeCollectorComponent forked)
	{
		throw new NotSupportedException();
	}

	protected override void ResetPooledInstanceStateIfNecessary()
	{
		throw new NotImplementedException();
	}

	protected override void SyncPooledInstanceWithParent(TypeCollectorComponent parent)
	{
		throw new NotImplementedException();
	}

	protected override TypeCollectorComponent CreateEmptyInstance()
	{
		return new TypeCollectorComponent(isForkedInstance: true);
	}

	protected override TypeCollectorComponent CreateCopyInstance()
	{
		throw new NotSupportedException();
	}

	protected override TypeCollectorComponent CreatePooledInstance()
	{
		throw new NotImplementedException();
	}

	protected override TypeCollectorComponent ThisAsFull()
	{
		return this;
	}

	protected override ITypeCollectorResults GetResults()
	{
		ReadOnlyCollection<IIl2CppRuntimeType> sortedTypes;
		if (_phaseSortedItems == null)
		{
			sortedTypes = _data.Values.ToSortedCollection();
		}
		else
		{
			PhaseSortItemsToReduceFinalSortTime();
			sortedTypes = _phaseSortedItems.AsReadOnly();
		}
		Dictionary<IIl2CppRuntimeType, int> dictionary = new Dictionary<IIl2CppRuntimeType, int>(sortedTypes.Count, Il2CppRuntimeTypeEqualityComparer.Default);
		foreach (IIl2CppRuntimeType item in sortedTypes)
		{
			dictionary.Add(item, dictionary.Count);
		}
		return new Results(sortedTypes, dictionary);
	}

	protected override ITypeCollector GetNotAvailableWrite()
	{
		return new NotAvailable();
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out ITypeCollector writer, out object reader, out TypeCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out ITypeCollector writer, out object reader, out TypeCollectorComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out ITypeCollector writer, out object reader, out TypeCollectorComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out ITypeCollector writer, out object reader, out TypeCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}
}
