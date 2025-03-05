using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;

public class CollectGenericMethodPointerNames : ChunkedItemsWithPostProcessingFunc<GlobalSecondaryCollectionContext, Il2CppMethodSpec, CollectGenericMethodPointerNames.PartialMethodTable, ReadOnlyGenericMethodPointerNameTable>
{
	public class PartialMethodTable
	{
		public readonly List<ReadOnlyMethodPointerNameEntry> Items;

		public PartialMethodTable(List<ReadOnlyMethodPointerNameEntry> items)
		{
			Items = items;
		}
	}

	protected override string Name => "Collect Generic Method Pointer Table";

	protected override string PostProcessingSectionName => "Merge Generic Method Pointer Table";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return !context.Parameters.EmitMethodMap;
	}

	protected override string ProfilerDetailsForItem(ReadOnlyCollection<Il2CppMethodSpec> workerItem)
	{
		return "Collect Generic Method Pointer Table (Chunked)";
	}

	protected override PartialMethodTable ProcessItem(GlobalSecondaryCollectionContext context, ReadOnlyCollection<Il2CppMethodSpec> items)
	{
		List<ReadOnlyMethodPointerNameEntry> results = new List<ReadOnlyMethodPointerNameEntry>(items.Count);
		ReadOnlyContext readOnlyContext = context.GetReadOnlyContext();
		foreach (Il2CppMethodSpec item in items)
		{
			MethodReference method = item.GenericMethod;
			results.Add(new ReadOnlyMethodPointerNameEntry(method, MethodTables.MethodPointerNameFor(readOnlyContext, method)));
		}
		return new PartialMethodTable(results);
	}

	protected override ReadOnlyGenericMethodPointerNameTable CreateEmptyResult()
	{
		return null;
	}

	protected override ReadOnlyCollection<ReadOnlyCollection<Il2CppMethodSpec>> Chunk(GlobalSchedulingContext context, ReadOnlyCollection<Il2CppMethodSpec> items)
	{
		return items.Chunk(context.InputData.JobCount * 2);
	}

	protected override ReadOnlyGenericMethodPointerNameTable PostProcess(GlobalSecondaryCollectionContext context, ReadOnlyCollection<ResultData<ReadOnlyCollection<Il2CppMethodSpec>, PartialMethodTable>> data)
	{
		List<ReadOnlyMethodPointerNameEntry> finalResults = new List<ReadOnlyMethodPointerNameEntry>(context.Results.PrimaryWrite.GenericMethods.Count);
		foreach (ResultData<ReadOnlyCollection<Il2CppMethodSpec>, PartialMethodTable> datum in data)
		{
			finalResults.AddRange(datum.Result.Items);
		}
		return new ReadOnlyGenericMethodPointerNameTable(finalResults.AsReadOnly());
	}
}
