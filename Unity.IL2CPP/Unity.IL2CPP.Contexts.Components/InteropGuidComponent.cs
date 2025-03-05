using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Components;

public class InteropGuidComponent : ForkAndMergeListCollectorBase<IIl2CppRuntimeType, ReadOnlyCollection<IIl2CppRuntimeType>, IInteropGuidCollector, InteropGuidComponent>, IInteropGuidCollector
{
	private class NotAvailable : IInteropGuidCollector
	{
		public void Add(SourceWritingContext context, TypeReference type)
		{
			throw new NotSupportedException();
		}

		public void Add(SourceWritingContext context, IEnumerable<TypeReference> types)
		{
			throw new NotSupportedException();
		}
	}

	public void Add(SourceWritingContext context, TypeReference type)
	{
		AddInternal(context.Global.Collectors.Types.Add(type));
	}

	public void Add(SourceWritingContext context, IEnumerable<TypeReference> types)
	{
		ITypeCollector typeCollector = context.Global.Collectors.Types;
		foreach (TypeReference item in types)
		{
			AddInternal(typeCollector.Add(item));
		}
	}

	protected override InteropGuidComponent CreateEmptyInstance()
	{
		return new InteropGuidComponent();
	}

	protected override InteropGuidComponent CreateCopyInstance()
	{
		throw new NotSupportedException();
	}

	protected override InteropGuidComponent ThisAsFull()
	{
		return this;
	}

	protected override IInteropGuidCollector GetNotAvailableWrite()
	{
		return new NotAvailable();
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out IInteropGuidCollector writer, out object reader, out InteropGuidComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out IInteropGuidCollector writer, out object reader, out InteropGuidComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out IInteropGuidCollector writer, out object reader, out InteropGuidComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out IInteropGuidCollector writer, out object reader, out InteropGuidComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override ReadOnlyCollection<IIl2CppRuntimeType> SortItems(IEnumerable<IIl2CppRuntimeType> items)
	{
		return items.ToSortedCollection();
	}

	protected override ReadOnlyCollection<IIl2CppRuntimeType> BuildResults(ReadOnlyCollection<IIl2CppRuntimeType> sortedItem)
	{
		return sortedItem;
	}
}
