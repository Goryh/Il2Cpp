using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Attributes;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Debugger;
using Unity.IL2CPP.GenericsCollection;

namespace Unity.IL2CPP.AssemblyConversion.Phases;

internal static class PrimaryCollectionPhase
{
	public static void Run(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
	{
		TinyProfilerComponent tinyProfiler = context.Services.TinyProfiler;
		using (tinyProfiler.Section("PrimaryCollectionPhase"))
		{
			ReadOnlyGlobalPendingResults<IMetadataCollectionResults> pendingMetadataCollection = null;
			ReadOnlyPerAssemblyPendingResults<AssemblyAnalyticsData> pendingAnalyticsCollection = null;
			ReadOnlyGlobalPendingResults<ReadOnlyInflatedCollectionCollector> genericsCollectionResults;
			ReadOnlyGlobalPendingResults<GenericSharingAnalysisResults> genericSharingAnalysis;
			ReadOnlyPerAssemblyPendingResults<ReadOnlyCollectedAttributeSupportData> attributeSupportData;
			ReadOnlyPerAssemblyPendingResults<ISequencePointProvider> pendingSequencePointCollectionResults;
			ReadOnlyPerAssemblyPendingResults<ICatchPointProvider> pendingCatchPointCollectionResults;
			ReadOnlyPerAssemblyPendingResults<CollectedWindowsRuntimeData> windowsRuntimeData;
			using (IPhaseWorkScheduler<GlobalPrimaryCollectionContext> scheduler = PhaseWorkSchedulerFactory.ForPrimaryCollection(context))
			{
				using (tinyProfiler.Section("Scheduling"))
				{
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterGenericsCollection"))
					{
						genericsCollectionResults = new GenericsCollectionParallel().Schedule(scheduler, assemblies);
					}
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterCollectMetadata"))
					{
						pendingMetadataCollection = new CollectMetadata(assemblies).Schedule(scheduler, assemblies, assemblies);
					}
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterSharedGenerics"))
					{
						genericSharingAnalysis = new GenericSharingAnalysis().Schedule(scheduler, assemblies);
					}
					attributeSupportData = new AttributeSupportCollection().Schedule(scheduler, assemblies);
					pendingSequencePointCollectionResults = new SequencePointCollection().Schedule(scheduler, assemblies);
					pendingCatchPointCollectionResults = new CatchPointCollection().Schedule(scheduler, assemblies);
					windowsRuntimeData = new WindowsRuntimeDataCollection().Schedule(scheduler, assemblies);
					new CCWMarshalingFunctionCollection().Schedule(scheduler, assemblies);
					new AssemblyCollection().Schedule(scheduler, assemblies);
					pendingAnalyticsCollection = new AnalyticsCollection().Schedule(scheduler, assemblies);
				}
			}
			using (tinyProfiler.Section("Build Results"))
			{
				context.Results.SetPrimaryCollectionResults(new AssemblyConversionResults.PrimaryCollectionPhase(new SequencePointProviderCollection(pendingSequencePointCollectionResults.Result), new CatchPointCollectorCollection(pendingCatchPointCollectionResults.Result), genericsCollectionResults.Result, attributeSupportData.Result, context.Collectors.WindowsRuntimeTypeWithNames.Complete(), windowsRuntimeData.Result, context.Collectors.CCWMarshallingFunctions.Complete(), genericSharingAnalysis.Result, pendingMetadataCollection?.Result, pendingAnalyticsCollection.Result));
			}
		}
	}
}
