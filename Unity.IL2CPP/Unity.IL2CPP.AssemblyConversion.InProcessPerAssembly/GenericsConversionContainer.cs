using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.Generic;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.Steps;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Attributes;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.GenericsCollection;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.AssemblyConversion.InProcessPerAssembly;

public class GenericsConversionContainer : BaseConversionContainer
{
	public const string GenericsContainerCleanName = "Il2CppGenerics";

	private readonly ReadOnlyCollection<AssemblyDefinition> _allAssemblies;

	public override string Name => "Generics";

	public override string CleanName => "Il2CppGenerics";

	public GenericsConversionContainer(ReadOnlyCollection<AssemblyDefinition> allAssemblies, int index)
		: base(index)
	{
		_allAssemblies = allAssemblies;
	}

	public override bool IncludeTypeDefinitionInContext(TypeReference type)
	{
		if (type is TypeSpecification)
		{
			return !type.GetNonPinnedAndNonByReferenceType().IsGenericParameter;
		}
		return false;
	}

	protected override AssemblyConversionResults.PrimaryCollectionPhase PrimaryCollectionPhase(GlobalFullyForkedContext context, GenericSharingAnalysisResults genericSharingAnalysisResults)
	{
		ReadOnlyGlobalPendingResults<IMetadataCollectionResults> pendingMetadataCollection = null;
		ReadOnlyGlobalPendingResults<ReadOnlyInflatedCollectionCollector> genericsCollectionResults;
		using (IPhaseWorkScheduler<GlobalPrimaryCollectionContext> scheduler = CreateHackedScheduler(context.GlobalPrimaryCollectionContext, context.GlobalSchedulingContext))
		{
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterGenericsCollection"))
			{
				genericsCollectionResults = new GenericsCollectionParallel().Schedule(scheduler, _allAssemblies);
			}
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterCollectMetadata"))
			{
				pendingMetadataCollection = new CollectMetadata(_allAssemblies).Schedule(scheduler, _allAssemblies, _allAssemblies);
			}
		}
		using (context.Services.TinyProfiler.Section("Build Results"))
		{
			return new AssemblyConversionResults.PrimaryCollectionPhase(SequencePointProviderCollection.Empty, CatchPointCollectorCollection.Empty, genericsCollectionResults.Result, new Dictionary<AssemblyDefinition, ReadOnlyCollectedAttributeSupportData>().AsReadOnly(), context.Collectors.WindowsRuntimeTypeWithNames.Complete(), new Dictionary<AssemblyDefinition, CollectedWindowsRuntimeData>().AsReadOnly(), context.Collectors.CCWMarshallingFunctions.Complete(), genericSharingAnalysisResults, pendingMetadataCollection.Result, new Dictionary<AssemblyDefinition, AssemblyAnalyticsData>().AsReadOnly());
		}
	}

	protected override AssemblyConversionResults.PrimaryWritePhase PrimaryWritePhase(GlobalFullyForkedContext context)
	{
		using (IPhaseWorkScheduler<GlobalWriteContext> scheduler = CreateHackedScheduler(context.GlobalWriteContext, context.GlobalSchedulingContext))
		{
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteGenericMethods"))
			{
				new WriteGenericMethods().Schedule(scheduler);
			}
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteGenericInstanceTypes"))
			{
				new WriteGenericInstanceTypes().Schedule(scheduler);
			}
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteGenericComDefinitions"))
			{
				new WriteGenericComDefinitions().Schedule(scheduler);
			}
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteComCallableWrappers"))
			{
				new WriteComCallableWrappers().Schedule(scheduler);
			}
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteProjectedInterfacesByCCWs"))
			{
				new WriteProjectedInterfacesByComCallableWrappers().Schedule(scheduler);
			}
		}
		using (context.Services.TinyProfiler.Section("Build Results"))
		{
			return new AssemblyConversionResults.PrimaryWritePhase(context.Collectors.GenericMethodCollector.Complete(), context.Collectors.Methods.Complete(), context.Collectors.TypeCollector.Complete(), context.Collectors.ReversePInvokeWrappers.Complete(), context.Collectors.TypeMarshallingFunctions.Complete(), context.Collectors.WrappersForDelegateFromManagedToNative.Complete(), context.Collectors.InteropGuids.Complete(), context.Collectors.MetadataUsage.Complete(), new Dictionary<AssemblyDefinition, GenericContextCollection>().AsReadOnly(), context.Collectors.Symbols.Complete(), context.Collectors.MatchedAssemblyMethodSourceFiles.Complete());
		}
	}

	protected override AssemblyConversionResults.SecondaryCollectionPhase SecondaryCollectionPhase(GlobalFullyForkedContext context)
	{
		ReadOnlyGlobalPendingResults<ReadOnlyMethodTables> pendingMethodTables = null;
		ReadOnlyGlobalPendingResults<ReadOnlyFieldReferenceTable> pendingFieldReferenceTable = null;
		ReadOnlyGlobalPendingResults<ReadOnlyStringLiteralTable> pendingStringLiteralTable = null;
		ReadOnlyGlobalPendingResults<ReadOnlyGenericInstanceTable> pendingGenericInstanceCollection = null;
		ReadOnlyGlobalPendingResults<ReadOnlySortedMetadata> pendingSortedMetadata = null;
		ReadOnlyGlobalPendingResults<ReadOnlyGenericMethodPointerNameTable> pendingGenericMethodPointerNameTable = null;
		using (IPhaseWorkScheduler<GlobalSecondaryCollectionContext> scheduler = CreateHackedScheduler(context.GlobalSecondaryCollectionContext, context.GlobalSchedulingContext))
		{
			pendingSortedMetadata = new SortMetadata().Schedule(scheduler);
			pendingMethodTables = new CollectMethodTables().Schedule(scheduler);
			pendingGenericInstanceCollection = new CollectGenericInstances().Schedule(scheduler);
			pendingFieldReferenceTable = new CollectFieldReferences().Schedule(scheduler);
			pendingStringLiteralTable = new CollectStringLiterals().Schedule(scheduler);
			pendingGenericMethodPointerNameTable = new CollectGenericMethodPointerNames().Schedule(scheduler, scheduler.SchedulingContext.Results.PrimaryWrite.GenericMethods.SortedKeys);
		}
		using (context.Services.TinyProfiler.Section("Build Results"))
		{
			return new AssemblyConversionResults.SecondaryCollectionPhase(null, pendingMethodTables?.Result, null, pendingFieldReferenceTable?.Result, pendingStringLiteralTable?.Result, pendingGenericInstanceCollection?.Result, pendingSortedMetadata?.Result, null, pendingGenericMethodPointerNameTable?.Result);
		}
	}

	protected override AssemblyConversionResults.SecondaryWritePhasePart1 SecondaryWritePhasePart1(GlobalFullyForkedContext context)
	{
		using (context.Services.TinyProfiler.Section("Build Results"))
		{
			return new AssemblyConversionResults.SecondaryWritePhasePart1(context.Collectors.IndirectCalls.Complete());
		}
	}

	protected override AssemblyConversionResults.SecondaryWritePhasePart3 SecondaryWritePhasePart3(GlobalFullyForkedContext context)
	{
		UnresolvedIndirectCallsTableInfo virtualCallTables;
		using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteUnresolvedStubs"))
		{
			SecondaryWriteSteps.WriteUnresolvedIndirectCalls(context.GlobalWriteContext, out virtualCallTables);
		}
		using (context.Services.TinyProfiler.Section("Build Results"))
		{
			return new AssemblyConversionResults.SecondaryWritePhasePart3(virtualCallTables);
		}
	}

	protected override AssemblyConversionResults.SecondaryWritePhase SecondaryWritePhasePart4(GlobalFullyForkedContext context)
	{
		using (IPhaseWorkScheduler<GlobalWriteContext> scheduler = CreateHackedScheduler(context.GlobalWriteContext, context.GlobalSchedulingContext))
		{
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteMetadata"))
			{
				new WriteGlobalMetadata().Schedule(scheduler);
			}
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteCodeRegistration"))
			{
				new WriteFullPerAssemblyCodeRegistration().Schedule(scheduler);
			}
			new WriteGenericsPseudoCodeGenModule(CleanName).Schedule(scheduler);
			new WriteMethodMap(Array.Empty<AssemblyDefinition>().AsReadOnly()).Schedule(scheduler, context.Results.SecondaryCollection.GenericMethodPointerNameTable?.Items);
			new WriteLineMapping().Schedule(scheduler);
		}
		using (context.Services.TinyProfiler.Section("Build Results"))
		{
			return new AssemblyConversionResults.SecondaryWritePhase(context.StatefulServices.PathFactory.Complete());
		}
	}

	protected override void CompletionPhase(GlobalFullyForkedContext context)
	{
	}
}
