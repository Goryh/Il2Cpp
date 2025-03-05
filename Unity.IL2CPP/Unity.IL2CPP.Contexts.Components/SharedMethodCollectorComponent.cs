using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.Ordering;

namespace Unity.IL2CPP.Contexts.Components;

public class SharedMethodCollectorComponent : ForkAndMergeTableCollectorBase<MethodReference, List<MethodReference>, ReadOnlyCollection<MethodReference>, SharedMethodCollection, ISharedMethodCollector, object, SharedMethodCollectorComponent>, ISharedMethodCollector
{
	private class NotAvailable : ISharedMethodCollector
	{
		public void AddSharedMethod(MethodReference sharedMethod, MethodReference actualMethod)
		{
			throw new NotSupportedException();
		}
	}

	public void AddSharedMethod(MethodReference sharedMethod, MethodReference actualMethod)
	{
		if (!TryGetValueInternal(sharedMethod, out var values))
		{
			values = new List<MethodReference>();
			AddInternal(sharedMethod, values);
		}
		values.Add(actualMethod);
	}

	protected override ReadOnlyCollection<MethodReference> ValueToResultValue(List<MethodReference> value)
	{
		return value.AsReadOnly();
	}

	protected override ReadOnlyCollection<KeyValuePair<MethodReference, TSortValue>> SortTable<TSortValue>(ReadOnlyDictionary<MethodReference, TSortValue> table)
	{
		return table.ItemsSortedByKey();
	}

	protected override ReadOnlyCollection<MethodReference> SortKeys(ICollection<MethodReference> items)
	{
		return items.ToSortedCollection();
	}

	protected override string DumpStateValueToString(List<MethodReference> value)
	{
		StringBuilder builder = new StringBuilder();
		if (value == null)
		{
			builder.AppendLine("  null");
		}
		else if (value.Count == 0)
		{
			builder.AppendLine("  Empty");
		}
		else
		{
			StringBuilder stringBuilder = builder;
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder);
			handler.AppendLiteral("    Count = ");
			handler.AppendFormatted(value.Count);
			stringBuilder2.AppendLine(ref handler);
			foreach (MethodReference method in value)
			{
				stringBuilder = builder;
				StringBuilder stringBuilder3 = stringBuilder;
				handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder);
				handler.AppendLiteral("      ");
				handler.AppendFormatted(method.FullName);
				stringBuilder3.AppendLine(ref handler);
			}
		}
		return builder.ToString();
	}

	protected override SharedMethodCollection CreateResultObject(ReadOnlyDictionary<MethodReference, ReadOnlyCollection<MethodReference>> table, ReadOnlyCollection<KeyValuePair<MethodReference, ReadOnlyCollection<MethodReference>>> sortedItems, ReadOnlyCollection<MethodReference> sortedKeys)
	{
		return new SharedMethodCollection(table, sortedItems, sortedKeys);
	}

	protected override SharedMethodCollectorComponent CreateEmptyInstance()
	{
		return new SharedMethodCollectorComponent();
	}

	protected override SharedMethodCollectorComponent CreateCopyInstance()
	{
		throw new NotSupportedException();
	}

	protected override SharedMethodCollectorComponent ThisAsFull()
	{
		return this;
	}

	protected override object ThisAsRead()
	{
		throw new NotSupportedException();
	}

	protected override ISharedMethodCollector GetNotAvailableWrite()
	{
		return new NotAvailable();
	}

	protected override object GetNotAvailableRead()
	{
		throw new NotSupportedException();
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out ISharedMethodCollector writer, out object reader, out SharedMethodCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out ISharedMethodCollector writer, out object reader, out SharedMethodCollectorComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full, ForkMode.Empty, MergeMode.MergeValues);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out ISharedMethodCollector writer, out object reader, out SharedMethodCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out ISharedMethodCollector writer, out object reader, out SharedMethodCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override bool DoValuesConflictForAddMergeMode(List<MethodReference> thisValue, List<MethodReference> otherValue)
	{
		throw new NotSupportedException();
	}

	protected override List<MethodReference> MergeValuesForMergeMergeMode(List<MethodReference> thisValue, List<MethodReference> otherValue)
	{
		thisValue.AddRange(otherValue);
		return thisValue;
	}
}
