using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.Phases;

internal static class SecondaryCollectionPhase
{
	public static void Run(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
	{
		ReadOnlyGlobalPendingResults<ReadOnlyInvokerCollection> pendingInvokerCollectorResults = null;
		ReadOnlyGlobalPendingResults<ReadOnlyMethodTables> pendingMethodTables = null;
		ReadOnlyGlobalPendingResults<ReadOnlyInteropTable> pendingInteropDataTable = null;
		ReadOnlyGlobalPendingResults<ReadOnlyFieldReferenceTable> pendingFieldReferenceTable = null;
		ReadOnlyGlobalPendingResults<ReadOnlyStringLiteralTable> pendingStringLiteralTable = null;
		ReadOnlyGlobalPendingResults<ReadOnlyGenericInstanceTable> pendingGenericInstanceCollection = null;
		ReadOnlyGlobalPendingResults<ReadOnlySortedMetadata> pendingSortedMetadata = null;
		ReadOnlyPerAssemblyPendingResults<ReadOnlyMethodPointerNameTable> pendingMethodPointerNameTable = null;
		ReadOnlyGlobalPendingResults<ReadOnlyGenericMethodPointerNameTable> pendingGenericMethodPointerNameTable = null;
		TinyProfilerComponent tinyProfiler = context.Services.TinyProfiler;
		using (tinyProfiler.Section("SecondaryCollectionPhase"))
		{
			using (tinyProfiler.Section("Scheduling"))
			{
				using IPhaseWorkScheduler<GlobalSecondaryCollectionContext> scheduler = PhaseWorkSchedulerFactory.ForSecondaryCollection(context);
				pendingSortedMetadata = new SortMetadata().Schedule(scheduler);
				pendingInvokerCollectorResults = new CollectInvokers().Schedule(scheduler, assemblies, scheduler.SchedulingContext.Results.PrimaryWrite.GenericMethods.UnsortedKeys);
				pendingMethodTables = new CollectMethodTables().Schedule(scheduler);
				pendingInteropDataTable = new CollectInteropTable().Schedule(scheduler);
				pendingGenericInstanceCollection = new CollectGenericInstances().Schedule(scheduler);
				pendingFieldReferenceTable = new CollectFieldReferences().Schedule(scheduler);
				pendingStringLiteralTable = new CollectStringLiterals().Schedule(scheduler);
				pendingGenericMethodPointerNameTable = new CollectGenericMethodPointerNames().Schedule(scheduler, scheduler.SchedulingContext.Results.PrimaryWrite.GenericMethods.SortedKeys);
				pendingMethodPointerNameTable = new CollectMethodPointerNames().Schedule(scheduler, assemblies);
				new WarningsScanner().Schedule(scheduler, assemblies);
			}
			using (tinyProfiler.Section("Build Results"))
			{
				context.Results.SetSecondaryCollectionPhaseResults(new AssemblyConversionResults.SecondaryCollectionPhase(pendingInvokerCollectorResults?.Result, pendingMethodTables?.Result, pendingInteropDataTable?.Result, pendingFieldReferenceTable?.Result, pendingStringLiteralTable?.Result, pendingGenericInstanceCollection?.Result, pendingSortedMetadata?.Result, pendingMethodPointerNameTable?.Result, pendingGenericMethodPointerNameTable?.Result));
			}
		}
	}
}
