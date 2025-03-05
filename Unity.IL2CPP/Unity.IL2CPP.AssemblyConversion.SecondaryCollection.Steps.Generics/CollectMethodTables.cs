using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;

public class CollectMethodTables : ChunkedItemsWithPostProcessingFunc<GlobalSecondaryCollectionContext, Il2CppMethodSpec, CollectMethodTables.PartialMethodTable, ReadOnlyMethodTables>
{
	public class PartialMethodTable
	{
		public readonly List<TableData> MethodPointers = new List<TableData>();
	}

	public class GenericMethodPointerTableEntry
	{
		public readonly int Index;

		public readonly bool IsNull;

		private readonly MethodReference _method;

		private readonly bool _isSharedMethod;

		public GenericMethodPointerTableEntry(int index, MethodReference method, bool isNull, bool isSharedMethod)
		{
			Index = index;
			IsNull = isNull;
			_method = method;
			_isSharedMethod = isSharedMethod;
		}

		public string Name()
		{
			if (IsNull)
			{
				return "NULL";
			}
			if (_isSharedMethod)
			{
				return _method.CppName + "_gshared";
			}
			return _method.CppName;
		}
	}

	public class GenericMethodTableEntry
	{
		public readonly int PointerTableIndex;

		public readonly int AdjustorThunkTableIndex;

		public readonly uint TableIndex;

		public readonly Il2CppMethodSpec Method;

		public GenericMethodTableEntry(Il2CppMethodSpec method, int pointerTableIndex, int adjustorThunkTableIndex, uint tableIndex)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			PointerTableIndex = pointerTableIndex;
			AdjustorThunkTableIndex = adjustorThunkTableIndex;
			TableIndex = tableIndex;
			Method = method;
		}
	}

	public class GenericMethodAdjustorThunkTableEntry
	{
		public readonly int Index;

		private readonly MethodReference _method;

		public GenericMethodAdjustorThunkTableEntry(int index, MethodReference method)
		{
			Index = index;
			_method = method;
		}

		public string Name(ReadOnlyContext context)
		{
			return _method.NameForAdjustorThunk();
		}
	}

	public class TableData
	{
		private readonly Il2CppMethodSpec _genericMethodTableMethod;

		private readonly uint _genericMethodTableMethodIndex;

		private readonly bool _isNull;

		private readonly MethodReference _genericMethodPointerTableMethod;

		private readonly bool _hasAdjustorThunk;

		private readonly bool _isSharedMethod;

		private readonly int _hashCode;

		public bool NeedsAdjustorThunkTableEntry => _hasAdjustorThunk;

		private TableData(Il2CppMethodSpec genericMethodTableMethod, uint genericMethodTableMethodIndex)
		{
			_genericMethodTableMethod = genericMethodTableMethod;
			_genericMethodTableMethodIndex = genericMethodTableMethodIndex;
			_isNull = true;
			_hashCode = 0;
		}

		public TableData(Il2CppMethodSpec genericMethodTableMethod, MethodReference genericMethodPointerTableMethod, bool isSharedMethod, bool hasAdjustorThunk, uint genericMethodTableMethodIndex)
		{
			_genericMethodTableMethod = genericMethodTableMethod;
			_genericMethodTableMethodIndex = genericMethodTableMethodIndex;
			_genericMethodPointerTableMethod = genericMethodPointerTableMethod;
			_hasAdjustorThunk = hasAdjustorThunk;
			_isSharedMethod = isSharedMethod;
			_hashCode = _genericMethodPointerTableMethod.GetHashCode();
		}

		public static TableData CreateNull(Il2CppMethodSpec method, uint entryMethodIndex)
		{
			return new TableData(method, entryMethodIndex);
		}

		public GenericMethodPointerTableEntry CreateNullPointerTableEntry()
		{
			return new GenericMethodPointerTableEntry(0, null, isNull: true, isSharedMethod: false);
		}

		public GenericMethodPointerTableEntry CreatePointerTableEntry(int pointerTableIndex)
		{
			return new GenericMethodPointerTableEntry(pointerTableIndex, _genericMethodPointerTableMethod, _isNull, _isSharedMethod);
		}

		public GenericMethodAdjustorThunkTableEntry CreateAdjustorThunkTableEntry(int tableIndex)
		{
			return new GenericMethodAdjustorThunkTableEntry(tableIndex, _genericMethodPointerTableMethod);
		}

		public GenericMethodTableEntry CreateMethodTableEntry(int pointerTableIndex, int adjustorThunkTableIndex)
		{
			return new GenericMethodTableEntry(_genericMethodTableMethod, pointerTableIndex, adjustorThunkTableIndex, _genericMethodTableMethodIndex);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is TableData other))
			{
				return false;
			}
			if (_isNull != other._isNull)
			{
				return false;
			}
			if (_isNull && other._isNull)
			{
				return true;
			}
			if (_hasAdjustorThunk != other._hasAdjustorThunk)
			{
				return false;
			}
			if (_isSharedMethod != other._isSharedMethod)
			{
				return false;
			}
			return _genericMethodPointerTableMethod == other._genericMethodPointerTableMethod;
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}
	}

	protected override string Name => "Collect Method Tables";

	protected override string PostProcessingSectionName => "Merge Method Tables";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	public ReadOnlyGlobalPendingResults<ReadOnlyMethodTables> Schedule(IPhaseWorkScheduler<GlobalSecondaryCollectionContext> scheduler)
	{
		return Schedule(scheduler, scheduler.SchedulingContext.Results.PrimaryWrite.GenericMethods.SortedKeys);
	}

	protected override string ProfilerDetailsForItem(ReadOnlyCollection<Il2CppMethodSpec> workerItem)
	{
		return "Collect Method Tables (Chunk)";
	}

	protected override PartialMethodTable ProcessItem(GlobalSecondaryCollectionContext context, ReadOnlyCollection<Il2CppMethodSpec> item)
	{
		PartialMethodTable partialMethodTable = new PartialMethodTable();
		ReadOnlyContext readOnlyContext = context.GetReadOnlyContext();
		foreach (Il2CppMethodSpec genericMethod in item)
		{
			if (MethodTables.MethodNeedsTable(readOnlyContext, genericMethod))
			{
				TableData methodPointer = MethodPointerKeyFor(readOnlyContext, genericMethod);
				partialMethodTable.MethodPointers.Add(methodPointer);
			}
		}
		return partialMethodTable;
	}

	protected override ReadOnlyMethodTables CreateEmptyResult()
	{
		return null;
	}

	protected override ReadOnlyCollection<ReadOnlyCollection<Il2CppMethodSpec>> Chunk(GlobalSchedulingContext context, ReadOnlyCollection<Il2CppMethodSpec> items)
	{
		return items.Chunk(context.InputData.JobCount * 2);
	}

	protected override ReadOnlyMethodTables PostProcess(GlobalSecondaryCollectionContext context, ReadOnlyCollection<ResultData<ReadOnlyCollection<Il2CppMethodSpec>, PartialMethodTable>> data)
	{
		Dictionary<TableData, GenericMethodPointerTableEntry> methodPointers = new Dictionary<TableData, GenericMethodPointerTableEntry>();
		List<GenericMethodPointerTableEntry> orderedPointerTableValues = new List<GenericMethodPointerTableEntry>();
		List<GenericMethodAdjustorThunkTableEntry> orderedAdjustorThunkTableValues = new List<GenericMethodAdjustorThunkTableEntry>();
		List<GenericMethodTableEntry> orderedMethodTableValues = new List<GenericMethodTableEntry>();
		TableData nullKey = TableData.CreateNull(null, 0u);
		GenericMethodPointerTableEntry nullValue = nullKey.CreateNullPointerTableEntry();
		methodPointers.Add(nullKey, nullValue);
		orderedPointerTableValues.Add(nullValue);
		foreach (TableData genericMethodData in data.SelectMany((ResultData<ReadOnlyCollection<Il2CppMethodSpec>, PartialMethodTable> d) => d.Result.MethodPointers))
		{
			GenericMethodPointerTableEntry pointerTableValue = ProcessForMethodPointerTable(methodPointers, orderedPointerTableValues, genericMethodData);
			GenericMethodAdjustorThunkTableEntry adjustorThunkTableValue = ProcessForAdjustorThunk(orderedAdjustorThunkTableValues, genericMethodData);
			orderedMethodTableValues.Add(genericMethodData.CreateMethodTableEntry(pointerTableValue.Index, adjustorThunkTableValue?.Index ?? (-1)));
		}
		return new ReadOnlyMethodTables(orderedPointerTableValues, orderedAdjustorThunkTableValues, orderedMethodTableValues);
	}

	private static GenericMethodPointerTableEntry ProcessForMethodPointerTable(Dictionary<TableData, GenericMethodPointerTableEntry> table, List<GenericMethodPointerTableEntry> orderedValues, TableData item)
	{
		if (table.TryGetValue(item, out var existingIndex))
		{
			return existingIndex;
		}
		GenericMethodPointerTableEntry value = item.CreatePointerTableEntry(table.Count);
		table.Add(item, value);
		orderedValues.Add(value);
		return value;
	}

	private static GenericMethodAdjustorThunkTableEntry ProcessForAdjustorThunk(List<GenericMethodAdjustorThunkTableEntry> orderedValues, TableData item)
	{
		if (!item.NeedsAdjustorThunkTableEntry)
		{
			return null;
		}
		GenericMethodAdjustorThunkTableEntry value = item.CreateAdjustorThunkTableEntry(orderedValues.Count);
		orderedValues.Add(value);
		return value;
	}

	private static TableData MethodPointerKeyFor(ReadOnlyContext context, Il2CppMethodSpec method)
	{
		uint genericMethodIndex = context.Global.Results.PrimaryWrite.GenericMethods.GetIndex(method);
		MethodReference genericMethod = method.GenericMethod;
		if (MethodTables.MethodPointerIsNull(context, genericMethod))
		{
			return TableData.CreateNull(method, genericMethodIndex);
		}
		if (genericMethod.CanShare(context))
		{
			MethodReference sharedMethod = genericMethod.GetSharedMethod(context);
			return new TableData(method, sharedMethod, isSharedMethod: true, MethodWriter.HasAdjustorThunk(sharedMethod), genericMethodIndex);
		}
		return new TableData(method, genericMethod, isSharedMethod: false, MethodWriter.HasAdjustorThunk(genericMethod), genericMethodIndex);
	}
}
