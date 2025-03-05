using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.Ordering;
using Unity.IL2CPP.GenericsCollection.CodeFlow;

namespace Unity.IL2CPP.GenericsCollection;

public class ReadOnlyInflatedCollectionCollector
{
	public readonly ReadOnlyCollection<GenericInstanceType> Types;

	public readonly ReadOnlyCollection<GenericInstanceType> TypeDeclarations;

	public readonly ReadOnlyCollection<TypeReference> InstantiatedGenericsAndArrays;

	public readonly ReadOnlyCollection<GenericInstanceMethod> Methods;

	public readonly ReadOnlyHashSet<TypeReference> ExtraTypes;

	public static ReadOnlyInflatedCollectionCollector Empty => new ReadOnlyInflatedCollectionCollector();

	private ReadOnlyInflatedCollectionCollector()
	{
		Types = Array.Empty<GenericInstanceType>().AsReadOnly();
		TypeDeclarations = Array.Empty<GenericInstanceType>().AsReadOnly();
		Methods = Array.Empty<GenericInstanceMethod>().AsReadOnly();
		InstantiatedGenericsAndArrays = Array.Empty<TypeReference>().AsReadOnly();
	}

	internal ReadOnlyInflatedCollectionCollector(ReadOnlyContext context, IImmutableGenericsCollection inflatedCollectionCollector, CodeFlowCollectionResults codeFlowResults, ReadOnlyHashSet<TypeReference> extraTypes)
	{
		ITinyProfilerService tinyProfiler = context.Global.Services.TinyProfiler;
		using (tinyProfiler.Section("Sort Types"))
		{
			Types = inflatedCollectionCollector.Types.ToSortedCollection();
		}
		using (tinyProfiler.Section("Sort TypeDeclarations"))
		{
			TypeDeclarations = inflatedCollectionCollector.TypeDeclarations.ToSortedCollection();
		}
		using (tinyProfiler.Section("Sort Methods"))
		{
			Methods = inflatedCollectionCollector.Methods.ToSortedCollection();
		}
		using (tinyProfiler.Section("Sort InstantiatedGenericsAndArrays"))
		{
			InstantiatedGenericsAndArrays = codeFlowResults.InstantiatedGenericsAndArrays.ToSortedCollection();
		}
		ExtraTypes = extraTypes;
	}

	private ReadOnlyInflatedCollectionCollector(IEnumerable<ReadOnlyInflatedCollectionCollector> others)
	{
		Types = others.Aggregate(new HashSet<GenericInstanceType>(), delegate(HashSet<GenericInstanceType> accum, ReadOnlyInflatedCollectionCollector item)
		{
			accum.UnionWith(item.Types);
			return accum;
		}).ToSortedCollection();
		TypeDeclarations = others.Aggregate(new HashSet<GenericInstanceType>(), delegate(HashSet<GenericInstanceType> accum, ReadOnlyInflatedCollectionCollector item)
		{
			accum.UnionWith(item.TypeDeclarations);
			return accum;
		}).ToSortedCollection();
		InstantiatedGenericsAndArrays = others.Aggregate(new HashSet<TypeReference>(), delegate(HashSet<TypeReference> accum, ReadOnlyInflatedCollectionCollector item)
		{
			accum.UnionWith(item.InstantiatedGenericsAndArrays);
			return accum;
		}).ToSortedCollection();
		Methods = others.Aggregate(new HashSet<GenericInstanceMethod>(), delegate(HashSet<GenericInstanceMethod> accum, ReadOnlyInflatedCollectionCollector item)
		{
			accum.UnionWith(item.Methods);
			return accum;
		}).ToSortedCollection();
	}

	public static ReadOnlyInflatedCollectionCollector Merge(IEnumerable<ReadOnlyInflatedCollectionCollector> others)
	{
		return new ReadOnlyInflatedCollectionCollector(others);
	}
}
