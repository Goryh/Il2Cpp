using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.Ordering;
using Unity.IL2CPP.Diagnostics;
using Unity.IL2CPP.GenericSharing;

namespace Unity.IL2CPP.Contexts.Components;

public class RuntimeImplementedMethodWriterCollectorComponent : CompletableStatefulComponentBase<IRuntimeImplementedMethodWriterResults, IRuntimeImplementedMethodWriterCollector, RuntimeImplementedMethodWriterCollectorComponent>, IRuntimeImplementedMethodWriterCollector
{
	private struct RuntimeImplementedMethodData
	{
		public readonly GetGenericSharingDataDelegate GetGenericSharingData;

		public readonly WriteRuntimeImplementedMethodBodyDelegate WriteRuntimeImplementedMethodBody;

		public RuntimeImplementedMethodData(GetGenericSharingDataDelegate getGenericSharingData, WriteRuntimeImplementedMethodBodyDelegate writeRuntimeImplementedMethodBody)
		{
			GetGenericSharingData = getGenericSharingData;
			WriteRuntimeImplementedMethodBody = writeRuntimeImplementedMethodBody;
		}
	}

	private class NotAvailable : IRuntimeImplementedMethodWriterCollector
	{
		public void RegisterMethod(MethodDefinition method, GetGenericSharingDataDelegate getGenericSharingData, WriteRuntimeImplementedMethodBodyDelegate writeMethodBody)
		{
			throw new NotSupportedException();
		}
	}

	private class Results : IRuntimeImplementedMethodWriterResults
	{
		private ReadOnlyDictionary<MethodDefinition, RuntimeImplementedMethodData> _runtimeImplementedMethods;

		public Results(ReadOnlyDictionary<MethodDefinition, RuntimeImplementedMethodData> runtimeImplementedMethods)
		{
			_runtimeImplementedMethods = runtimeImplementedMethods;
		}

		public bool TryGetWriter(MethodDefinition method, out WriteRuntimeImplementedMethodBodyDelegate value)
		{
			if (_runtimeImplementedMethods.TryGetValue(method, out var data))
			{
				value = data.WriteRuntimeImplementedMethodBody;
				return true;
			}
			value = null;
			return false;
		}

		public bool TryGetGenericSharingDataFor(PrimaryCollectionContext context, MethodDefinition method, out IEnumerable<RuntimeGenericData> value)
		{
			if (_runtimeImplementedMethods.TryGetValue(method, out var data))
			{
				value = data.GetGenericSharingData(context);
				return true;
			}
			value = null;
			return false;
		}
	}

	private Dictionary<MethodDefinition, RuntimeImplementedMethodData> _runtimeImplementedMethods = new Dictionary<MethodDefinition, RuntimeImplementedMethodData>();

	public void RegisterMethod(MethodDefinition method, GetGenericSharingDataDelegate getGenericSharingData, WriteRuntimeImplementedMethodBodyDelegate writeMethodBody)
	{
		_runtimeImplementedMethods.Add(method, new RuntimeImplementedMethodData(getGenericSharingData, writeMethodBody));
	}

	protected override void DumpState(StringBuilder builder)
	{
		CollectorStateDumper.AppendCollection(builder, "_runtimeImplementedMethods", _runtimeImplementedMethods.Keys.ToSortedCollection());
	}

	protected override void HandleMergeForAdd(RuntimeImplementedMethodWriterCollectorComponent forked)
	{
		throw new NotSupportedException();
	}

	protected override void HandleMergeForMergeValues(RuntimeImplementedMethodWriterCollectorComponent forked)
	{
		throw new NotSupportedException();
	}

	protected override void ResetPooledInstanceStateIfNecessary()
	{
		throw new NotSupportedException();
	}

	protected override void SyncPooledInstanceWithParent(RuntimeImplementedMethodWriterCollectorComponent parent)
	{
		throw new NotSupportedException();
	}

	protected override RuntimeImplementedMethodWriterCollectorComponent CreateEmptyInstance()
	{
		return new RuntimeImplementedMethodWriterCollectorComponent();
	}

	protected override RuntimeImplementedMethodWriterCollectorComponent CreateCopyInstance()
	{
		throw new NotSupportedException();
	}

	protected override RuntimeImplementedMethodWriterCollectorComponent CreatePooledInstance()
	{
		throw new NotSupportedException();
	}

	protected override RuntimeImplementedMethodWriterCollectorComponent ThisAsFull()
	{
		return this;
	}

	protected override IRuntimeImplementedMethodWriterCollector GetNotAvailableWrite()
	{
		return new NotAvailable();
	}

	protected override IRuntimeImplementedMethodWriterResults GetResults()
	{
		return new Results(_runtimeImplementedMethods.AsReadOnly());
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out IRuntimeImplementedMethodWriterCollector writer, out object reader, out RuntimeImplementedMethodWriterCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out IRuntimeImplementedMethodWriterCollector writer, out object reader, out RuntimeImplementedMethodWriterCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out IRuntimeImplementedMethodWriterCollector writer, out object reader, out RuntimeImplementedMethodWriterCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out IRuntimeImplementedMethodWriterCollector writer, out object reader, out RuntimeImplementedMethodWriterCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPartialPerAssembly(in ForkingData data, out IRuntimeImplementedMethodWriterCollector writer, out object reader, out RuntimeImplementedMethodWriterCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForFullPerAssembly(in ForkingData data, out IRuntimeImplementedMethodWriterCollector writer, out object reader, out RuntimeImplementedMethodWriterCollectorComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}
}
