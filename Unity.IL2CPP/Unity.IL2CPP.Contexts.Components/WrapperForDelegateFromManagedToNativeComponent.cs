using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Components;

public class WrapperForDelegateFromManagedToNativeComponent : ForkAndMergeListCollectorBase<IIl2CppRuntimeType, ReadOnlyCollection<IIl2CppRuntimeType>, IWrapperForDelegateFromManagedToNativeCollector, WrapperForDelegateFromManagedToNativeComponent>, IWrapperForDelegateFromManagedToNativeCollector
{
	private class NotAvailable : IWrapperForDelegateFromManagedToNativeCollector
	{
		public void Add(SourceWritingContext context, MethodReference method)
		{
			throw new NotSupportedException();
		}
	}

	public void Add(SourceWritingContext context, MethodReference method)
	{
		AddInternal(context.Global.Collectors.Types.Add(method.DeclaringType));
	}

	protected override WrapperForDelegateFromManagedToNativeComponent CreateEmptyInstance()
	{
		return new WrapperForDelegateFromManagedToNativeComponent();
	}

	protected override WrapperForDelegateFromManagedToNativeComponent CreateCopyInstance()
	{
		throw new NotSupportedException();
	}

	protected override WrapperForDelegateFromManagedToNativeComponent ThisAsFull()
	{
		return this;
	}

	protected override IWrapperForDelegateFromManagedToNativeCollector GetNotAvailableWrite()
	{
		return new NotAvailable();
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out IWrapperForDelegateFromManagedToNativeCollector writer, out object reader, out WrapperForDelegateFromManagedToNativeComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out IWrapperForDelegateFromManagedToNativeCollector writer, out object reader, out WrapperForDelegateFromManagedToNativeComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out IWrapperForDelegateFromManagedToNativeCollector writer, out object reader, out WrapperForDelegateFromManagedToNativeComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out IWrapperForDelegateFromManagedToNativeCollector writer, out object reader, out WrapperForDelegateFromManagedToNativeComponent full)
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
