using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.Ordering;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Global;

public class SortMetadata : ScheduledStep
{
	protected override string Name => "Sort Metadata";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	public ReadOnlyGlobalPendingResults<ReadOnlySortedMetadata> Schedule(IPhaseWorkScheduler<GlobalSecondaryCollectionContext> scheduler)
	{
		using (CreateProfilerSectionAroundScheduling(scheduler.SchedulingContext, scheduler.WorkIsDoneOnDifferentThread))
		{
			GlobalPendingResults<ReadOnlySortedMetadata> pendingResults = new GlobalPendingResults<ReadOnlySortedMetadata>();
			if (Skip(scheduler.SchedulingContext))
			{
				pendingResults.SetResults(CreateEmptyResult());
				return new ReadOnlyGlobalPendingResults<ReadOnlySortedMetadata>(pendingResults);
			}
			ReadOnlyCollection<Func<ReadOnlyContext, object>> sortFunctions = new Func<ReadOnlyContext, object>[2] { SortMethods, GroupTypes }.AsReadOnly();
			scheduler.EnqueueItemsAndContinueWithResults(scheduler.QueuingContext, sortFunctions, ProcessSortFunction, CreateResult, pendingResults);
			return new ReadOnlyGlobalPendingResults<ReadOnlySortedMetadata>(pendingResults);
		}
	}

	private void CreateResult(WorkItemData<GlobalSecondaryCollectionContext, ReadOnlyCollection<ResultData<Func<ReadOnlyContext, object>, object>>, GlobalPendingResults<ReadOnlySortedMetadata>> data)
	{
		ReadOnlySortedMetadata result = new ReadOnlySortedMetadata((ReadOnlyCollection<MethodReference>)data.Item[0].Result, (ReadOnlyCollection<IGrouping<TypeReference, IIl2CppRuntimeType>>)data.Item[1].Result);
		data.Tag.SetResults(result);
	}

	private object ProcessSortFunction(WorkItemData<GlobalSecondaryCollectionContext, Func<ReadOnlyContext, object>, GlobalPendingResults<ReadOnlySortedMetadata>> data)
	{
		using (data.Context.Services.TinyProfiler.Section(Name))
		{
			return data.Item(data.Context.GetReadOnlyContext());
		}
	}

	private ReadOnlySortedMetadata CreateEmptyResult()
	{
		return null;
	}

	private static object SortMethods(ReadOnlyContext context)
	{
		using (context.Global.Services.TinyProfiler.Section("Sort Method Usages"))
		{
			return AllMethodsThatNeedRuntimeMetadata(context, context.Global.Results.PrimaryWrite.MetadataUsage).ToSortedCollection();
		}
	}

	private static object GroupTypes(ReadOnlyContext context)
	{
		using (context.Global.Services.TinyProfiler.Section("Group Types"))
		{
			return (from entry in context.Global.Results.PrimaryWrite.Types.SortedItems
				group entry by entry.Type.GetNonPinnedAndNonByReferenceType()).ToArray().AsReadOnly();
		}
	}

	private static HashSet<MethodReference> AllMethodsThatNeedRuntimeMetadata(ReadOnlyContext context, IMetadataUsageCollectorResults metadataUsages)
	{
		HashSet<MethodReference> hashSet = new HashSet<MethodReference>();
		hashSet.UnionWith(metadataUsages.GetInflatedMethods());
		hashSet.UnionWith(context.Global.Results.PrimaryWrite.ReversePInvokeWrappers.SortedKeys.Where(ReversePInvokeMethodBodyWriter.IsReversePInvokeMethodThatMustBeGenerated));
		return hashSet;
	}
}
