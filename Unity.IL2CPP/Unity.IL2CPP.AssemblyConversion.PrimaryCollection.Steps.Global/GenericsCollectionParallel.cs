using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Generics;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Visitor;
using Unity.IL2CPP.GenericsCollection;
using Unity.IL2CPP.GenericsCollection.CodeFlow;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global;

public class GenericsCollectionParallel : ScheduledStep
{
	private class AccumulatedData
	{
		public readonly ReadOnlyCollection<AssemblyDefinition> Assemblies;

		public readonly SimpleGenericsCollector Generics;

		public CodeFlowCollectionResults CodeFlowCollectionResults;

		public int GenericVirtualMethodsIteration;

		public readonly GlobalPendingResults<ReadOnlyInflatedCollectionCollector> Results;

		public AccumulatedData(GlobalPendingResults<ReadOnlyInflatedCollectionCollector> results, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			Results = results;
			Assemblies = assemblies;
			Generics = new SimpleGenericsCollector();
		}
	}

	private class IterativeWorkItem
	{
		public readonly IImmutableGenericsCollection LastIterationResults;

		public readonly ReadOnlyCollection<GenericInstanceType> TypesToProcess;

		public readonly ReadOnlyCollection<GenericInstanceMethod> MethodsToProcess;

		public IterativeWorkItem(ReadOnlyCollection<GenericInstanceType> typesToProcess, ReadOnlyCollection<GenericInstanceMethod> methodsToProcess, IImmutableGenericsCollection lastIterationResults)
		{
			LastIterationResults = lastIterationResults;
			TypesToProcess = typesToProcess;
			MethodsToProcess = methodsToProcess;
		}
	}

	private const int kJobCountMultiplier = 2;

	protected override string Name => "Generics Collection Parallel";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	public ReadOnlyGlobalPendingResults<ReadOnlyInflatedCollectionCollector> Schedule(IPhaseWorkScheduler<GlobalPrimaryCollectionContext> scheduler, ReadOnlyCollection<AssemblyDefinition> items)
	{
		using (CreateProfilerSectionAroundScheduling(scheduler.SchedulingContext, scheduler.WorkIsDoneOnDifferentThread))
		{
			GlobalPendingResults<ReadOnlyInflatedCollectionCollector> pendingResults = new GlobalPendingResults<ReadOnlyInflatedCollectionCollector>();
			if (Skip(scheduler.SchedulingContext))
			{
				pendingResults.SetResults(CreateEmptyResult());
				return new ReadOnlyGlobalPendingResults<ReadOnlyInflatedCollectionCollector>(pendingResults);
			}
			scheduler.EnqueueItemsAndContinueWithResults(scheduler.QueuingContext, PartitionWorkForInitialPhase(items, scheduler.SchedulingContext.InputData.JobCount), InitialPhaseWorker, InitialPhaseMerge, new AccumulatedData(pendingResults, items));
			return new ReadOnlyGlobalPendingResults<ReadOnlyInflatedCollectionCollector>(pendingResults);
		}
	}

	private static ReadOnlyCollection<ReadOnlyCollection<TypeDefinition>> PartitionWorkForInitialPhase(ReadOnlyCollection<AssemblyDefinition> items, int jobCount)
	{
		return items.SelectMany((AssemblyDefinition item) => item.GetAllTypes()).ToArray().Chunk(jobCount * 2);
	}

	private static SimpleGenericsCollector InitialPhaseWorker(WorkItemData<GlobalPrimaryCollectionContext, ReadOnlyCollection<TypeDefinition>, AccumulatedData> workerData)
	{
		using (workerData.Context.Services.TinyProfiler.Section("Generics Initial Collection"))
		{
			SimpleGenericsCollector generics = new SimpleGenericsCollector();
			GenericContextFreeVisitor visitor = new GenericContextFreeVisitor(workerData.Context.CreateCollectionContext(), generics);
			foreach (TypeDefinition item in workerData.Item)
			{
				item.Accept(visitor);
			}
			return generics;
		}
	}

	private static void InitialPhaseMerge(WorkItemData<GlobalPrimaryCollectionContext, ReadOnlyCollection<ResultData<ReadOnlyCollection<TypeDefinition>, SimpleGenericsCollector>>, AccumulatedData> workerData)
	{
		using (workerData.Context.Services.TinyProfiler.Section("Generics Initial Merge"))
		{
			SimpleGenericsCollector merged = workerData.Tag.Generics;
			foreach (ResultData<ReadOnlyCollection<TypeDefinition>, SimpleGenericsCollector> item2 in workerData.Item)
			{
				merged.Merge(item2.Result);
			}
			PrimaryCollectionContext collectionContext = workerData.Context.CreateCollectionContext();
			CodeFlowCollectionResults codeFlowCollectionResults = GenericCodeFlowGraphCollector.Collect(collectionContext, workerData.Tag.Assemblies);
			foreach (GenericInstanceType type in codeFlowCollectionResults.GenericTypes)
			{
				if (merged.AddTypeDeclaration(type))
				{
					GenericContextAwareVisitor.ProcessGenericType(collectionContext, type, merged);
				}
			}
			workerData.Tag.CodeFlowCollectionResults = codeFlowCollectionResults;
			workerData.Context.Services.Scheduler.EnqueueItemsAndContinueWithResults(workerData.Context, PartitionWorkForIteration(merged.Types, merged.Methods, workerData.Tag.Generics, workerData.Context.InputData.JobCount), IterativeWorker, IterativeMerge, workerData.Tag);
		}
	}

	private static ReadOnlyCollection<IterativeWorkItem> PartitionWorkForIteration(ICollection<GenericInstanceType> types, ICollection<GenericInstanceMethod> methods, IImmutableGenericsCollection lastIterationResults, int jobCount)
	{
		List<IterativeWorkItem> results = new List<IterativeWorkItem>();
		ReadOnlyCollection<GenericInstanceMethod> emptyMethods = Array.Empty<GenericInstanceMethod>().AsReadOnly();
		foreach (ReadOnlyCollection<GenericInstanceType> chunk in types.Chunk(jobCount * 2))
		{
			results.Add(new IterativeWorkItem(chunk, emptyMethods, lastIterationResults));
		}
		ReadOnlyCollection<GenericInstanceType> emptyTypes = Array.Empty<GenericInstanceType>().AsReadOnly();
		foreach (ReadOnlyCollection<GenericInstanceMethod> chunk2 in methods.Chunk(jobCount * 2))
		{
			results.Add(new IterativeWorkItem(emptyTypes, chunk2, lastIterationResults));
		}
		if (results.Count == 0)
		{
			results.Add(new IterativeWorkItem(emptyTypes, emptyMethods, lastIterationResults));
		}
		return results.AsReadOnly();
	}

	private static SimpleGenericsCollector IterativeWorker(WorkItemData<GlobalPrimaryCollectionContext, IterativeWorkItem, AccumulatedData> workerData)
	{
		using (workerData.Context.Services.TinyProfiler.Section("Generics Iteration", workerData.Tag.GenericVirtualMethodsIteration.ToString()))
		{
			return GenericsCollector.IterateOnce(workerData.Context.CreateCollectionContext(), workerData.Item.LastIterationResults, workerData.Item.TypesToProcess, workerData.Item.MethodsToProcess);
		}
	}

	private static void IterativeMerge(WorkItemData<GlobalPrimaryCollectionContext, ReadOnlyCollection<ResultData<IterativeWorkItem, SimpleGenericsCollector>>, AccumulatedData> workerData)
	{
		ITinyProfilerService tinyProfiler = workerData.Context.Services.TinyProfiler;
		using (tinyProfiler.Section("Generics Iteration Merge"))
		{
			ReadOnlyCollection<AssemblyDefinition> assemblies = workerData.Tag.Assemblies;
			SimpleGenericsCollector results = workerData.Tag.Generics;
			SimpleGenericsCollector newResults = MergeIterationResults(workerData, results);
			if (EnqueueIterativeWork(workerData, newResults.Types, newResults.Methods))
			{
				return;
			}
			PrimaryCollectionContext collectionContext = workerData.Context.CreateCollectionContext();
			if (workerData.Tag.GenericVirtualMethodsIteration < collectionContext.Global.Results.Initialize.GenericLimits.VirtualMethodIterations)
			{
				workerData.Tag.GenericVirtualMethodsIteration++;
				using (tinyProfiler.Section("Collect Generic Virtual Methods", workerData.Tag.GenericVirtualMethodsIteration.ToString()))
				{
					if (EnqueueIterativeWork(workerData, Array.Empty<GenericInstanceType>(), GenericsCollector.CollectGenericVirtualMethods(collectionContext, assemblies, results)))
					{
						return;
					}
				}
			}
			Complete(workerData, collectionContext, assemblies, results);
		}
	}

	private static void EnqueueBackgroundDataModelInitialization(WorkItemData<GlobalPrimaryCollectionContext, ReadOnlyCollection<ResultData<IterativeWorkItem, SimpleGenericsCollector>>, AccumulatedData> workerData, SimpleGenericsCollector results)
	{
		using (workerData.Context.Services.TinyProfiler.Section("Enqueue Background Init"))
		{
			workerData.Context.Services.Scheduler.EnqueueItems<GlobalPrimaryCollectionContext, ReadOnlyCollection<GenericInstanceMethod>, object>(workerData.Context, results.Methods.Chunk(workerData.Context.InputData.JobCount), CacheCppNames, null);
			workerData.Context.Services.Scheduler.EnqueueItems<GlobalPrimaryCollectionContext, ReadOnlyCollection<GenericInstanceType>, object>(workerData.Context, results.Types.Chunk(workerData.Context.InputData.JobCount), CacheCppNames, null);
			workerData.Context.Services.Scheduler.EnqueueItems<GlobalPrimaryCollectionContext, ReadOnlyCollection<GenericInstanceType>, object>(workerData.Context, results.TypeDeclarations.Chunk(workerData.Context.InputData.JobCount), CacheCppNames, null);
		}
	}

	private static void EnqueuePostProcessingWork(WorkItemData<GlobalPrimaryCollectionContext, ReadOnlyCollection<ResultData<IterativeWorkItem, SimpleGenericsCollector>>, AccumulatedData> workerData, IImmutableGenericsCollection results)
	{
		foreach (ReadOnlyCollection<GenericInstanceType> chunk in results.Types.Chunk(workerData.Context.InputData.JobCount))
		{
			workerData.Context.Services.Scheduler.EnqueueStep(workerData.Context, new CollectGenericMethodsFromTypes(chunk));
		}
		workerData.Context.Services.Scheduler.EnqueueStep(workerData.Context, new CollectGenericMethodsFromMethods(results.Methods));
		workerData.Context.Services.Scheduler.EnqueueStep(workerData.Context, new WindowsRuntimeDataCollectionForGenerics(results));
		workerData.Context.Services.Scheduler.EnqueueStep(workerData.Context, new CCWMarshallingFunctionsCollectionForGenerics(results, workerData.Tag.CodeFlowCollectionResults));
	}

	private static void CacheCppNames(WorkItemData<GlobalPrimaryCollectionContext, ReadOnlyCollection<GenericInstanceType>, object> cacheData)
	{
		using (cacheData.Context.Services.TinyProfiler.Section("Cache CppNames"))
		{
			foreach (GenericInstanceType item in cacheData.Item)
			{
				_ = item.CppName;
			}
		}
	}

	private static void CacheCppNames(WorkItemData<GlobalPrimaryCollectionContext, ReadOnlyCollection<GenericInstanceMethod>, object> cacheData)
	{
		using (cacheData.Context.Services.TinyProfiler.Section("Cache CppNames"))
		{
			foreach (GenericInstanceMethod item in cacheData.Item)
			{
				_ = item.CppName;
			}
		}
	}

	private static bool EnqueueIterativeWork(WorkItemData<GlobalPrimaryCollectionContext, ReadOnlyCollection<ResultData<IterativeWorkItem, SimpleGenericsCollector>>, AccumulatedData> workerData, ICollection<GenericInstanceType> typesToProcess, ICollection<GenericInstanceMethod> methodsToProcess)
	{
		if (typesToProcess.Count == 0 && methodsToProcess.Count == 0)
		{
			return false;
		}
		workerData.Context.Services.Scheduler.EnqueueItemsAndContinueWithResults(workerData.Context, PartitionWorkForIteration(typesToProcess, methodsToProcess, workerData.Tag.Generics, workerData.Context.InputData.JobCount), IterativeWorker, IterativeMerge, workerData.Tag);
		return true;
	}

	private static SimpleGenericsCollector MergeIterationResults(WorkItemData<GlobalPrimaryCollectionContext, ReadOnlyCollection<ResultData<IterativeWorkItem, SimpleGenericsCollector>>, AccumulatedData> workerData, SimpleGenericsCollector results)
	{
		using (workerData.Context.Services.TinyProfiler.Section("Merge Results"))
		{
			SimpleGenericsCollector newResults = new SimpleGenericsCollector();
			foreach (SimpleGenericsCollector newResult in workerData.Item.Select((ResultData<IterativeWorkItem, SimpleGenericsCollector> item) => item.Result))
			{
				newResults.Merge(newResult);
			}
			results.Merge(newResults);
			return newResults;
		}
	}

	private static void Complete(WorkItemData<GlobalPrimaryCollectionContext, ReadOnlyCollection<ResultData<IterativeWorkItem, SimpleGenericsCollector>>, AccumulatedData> workerData, PrimaryCollectionContext collectionContext, ReadOnlyCollection<AssemblyDefinition> assemblies, SimpleGenericsCollector results)
	{
		ITinyProfilerService tinyProfiler = collectionContext.Global.Services.TinyProfiler;
		ReadOnlyHashSet<TypeReference> extraTypes;
		using (tinyProfiler.Section("AddExtraTypes"))
		{
			SimpleGenericsCollector extraTypesCollection = GenericsCollector.CollectExtraTypes(collectionContext, assemblies, results, out extraTypes);
			GenericsCollector.IterateToCompletion(collectionContext, results, extraTypesCollection.Types, extraTypesCollection.Methods);
		}
		EnqueuePostProcessingWork(workerData, results);
		EnqueueBackgroundDataModelInitialization(workerData, results);
		ReadOnlyInflatedCollectionCollector readOnlyGenericsCollection;
		using (tinyProfiler.Section("Create ReadOnly Results"))
		{
			readOnlyGenericsCollection = new ReadOnlyInflatedCollectionCollector(collectionContext, results, workerData.Tag.CodeFlowCollectionResults, extraTypes);
		}
		workerData.Tag.Results.SetResults(readOnlyGenericsCollection);
	}

	private ReadOnlyInflatedCollectionCollector CreateEmptyResult()
	{
		return null;
	}
}
