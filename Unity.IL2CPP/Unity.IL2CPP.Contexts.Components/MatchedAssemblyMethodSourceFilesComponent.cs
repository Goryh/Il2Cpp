using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;

namespace Unity.IL2CPP.Contexts.Components;

public class MatchedAssemblyMethodSourceFilesComponent : ForkAndMergeHashSetCollectorBase<NPath, ReadOnlyCollection<NPath>, IMatchedAssemblyMethodSourceFilesCollector, MatchedAssemblyMethodSourceFilesComponent>, IMatchedAssemblyMethodSourceFilesCollector
{
	private class NotAvailable : IMatchedAssemblyMethodSourceFilesCollector
	{
		public void Add(NPath fileName)
		{
			throw new NotSupportedException();
		}
	}

	public void Add(NPath fileName)
	{
		AddInternal(fileName);
	}

	protected override MatchedAssemblyMethodSourceFilesComponent CreateEmptyInstance()
	{
		return new MatchedAssemblyMethodSourceFilesComponent();
	}

	protected override MatchedAssemblyMethodSourceFilesComponent CreateCopyInstance()
	{
		throw new NotSupportedException();
	}

	protected override MatchedAssemblyMethodSourceFilesComponent ThisAsFull()
	{
		return this;
	}

	protected override IMatchedAssemblyMethodSourceFilesCollector GetNotAvailableWrite()
	{
		return new NotAvailable();
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out IMatchedAssemblyMethodSourceFilesCollector writer, out object reader, out MatchedAssemblyMethodSourceFilesComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out IMatchedAssemblyMethodSourceFilesCollector writer, out object reader, out MatchedAssemblyMethodSourceFilesComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out IMatchedAssemblyMethodSourceFilesCollector writer, out object reader, out MatchedAssemblyMethodSourceFilesComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out IMatchedAssemblyMethodSourceFilesCollector writer, out object reader, out MatchedAssemblyMethodSourceFilesComponent full)
	{
		NotAvailableFork(in data, out writer, out reader, out full);
	}

	protected override void ForkForFullPerAssembly(in ForkingData data, out IMatchedAssemblyMethodSourceFilesCollector writer, out object reader, out MatchedAssemblyMethodSourceFilesComponent full)
	{
		WriteOnlyFork(in data, out writer, out reader, out full);
	}

	protected override ReadOnlyCollection<NPath> SortItems(IEnumerable<NPath> items)
	{
		return items.ToSortedCollection();
	}

	protected override ReadOnlyCollection<NPath> BuildResults(ReadOnlyCollection<NPath> sortedItem)
	{
		return sortedItem;
	}
}
