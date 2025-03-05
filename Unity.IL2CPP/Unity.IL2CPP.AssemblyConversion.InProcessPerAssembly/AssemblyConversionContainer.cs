using System;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.Steps;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Attributes;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Debugger;
using Unity.IL2CPP.GenericsCollection;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.AssemblyConversion.InProcessPerAssembly;

public class AssemblyConversionContainer : BaseConversionContainer
{
	private readonly ReadOnlyCollection<AssemblyDefinition> _assemblies;

	private readonly bool _isEntryAssembly;

	public override string Name { get; }

	public override string CleanName { get; }

	public AssemblyConversionContainer(AssemblyDefinition definition, bool isEntryAssembly, string name, string cleanName, int index)
		: base(index)
	{
		_assemblies = new AssemblyDefinition[1] { definition }.AsReadOnly();
		_isEntryAssembly = isEntryAssembly;
		Name = name;
		CleanName = cleanName;
	}

	public override bool IncludeTypeDefinitionInContext(TypeReference type)
	{
		TypeReference nonPinnedAndNonByReferenceType = type.GetNonPinnedAndNonByReferenceType();
		if (nonPinnedAndNonByReferenceType.IsGenericParameter)
		{
			return ((GenericParameter)nonPinnedAndNonByReferenceType).Module.Assembly == _assemblies[0];
		}
		if (!(type is TypeSpecification))
		{
			return type.Resolve().Module.Assembly == _assemblies[0];
		}
		return true;
	}

	protected override AssemblyConversionResults.PrimaryCollectionPhase PrimaryCollectionPhase(GlobalFullyForkedContext context, GenericSharingAnalysisResults genericSharingAnalysisResults)
	{
		ReadOnlyPerAssemblyPendingResults<ReadOnlyCollectedAttributeSupportData> attributeSupportData;
		ReadOnlyPerAssemblyPendingResults<ISequencePointProvider> pendingSequencePointCollectionResults;
		ReadOnlyPerAssemblyPendingResults<ICatchPointProvider> pendingCatchPointCollectionResults;
		ReadOnlyPerAssemblyPendingResults<CollectedWindowsRuntimeData> windowsRuntimeData;
		ReadOnlyPerAssemblyPendingResults<AssemblyAnalyticsData> analyticsData;
		using (IPhaseWorkScheduler<GlobalPrimaryCollectionContext> scheduler = CreateHackedScheduler(context.GlobalPrimaryCollectionContext, context.GlobalSchedulingContext))
		{
			attributeSupportData = new AttributeSupportCollection().Schedule(scheduler, _assemblies);
			pendingSequencePointCollectionResults = new SequencePointCollection().Schedule(scheduler, _assemblies);
			pendingCatchPointCollectionResults = new CatchPointCollection().Schedule(scheduler, _assemblies);
			windowsRuntimeData = new WindowsRuntimeDataCollection().Schedule(scheduler, _assemblies);
			new CCWMarshalingFunctionCollection().Schedule(scheduler, _assemblies);
			new AssemblyCollection().Schedule(scheduler, _assemblies);
			analyticsData = new AnalyticsCollection().Schedule(scheduler, _assemblies);
		}
		using (context.Services.TinyProfiler.Section("Build Results"))
		{
			return new AssemblyConversionResults.PrimaryCollectionPhase(new SequencePointProviderCollection(pendingSequencePointCollectionResults.Result), new CatchPointCollectorCollection(pendingCatchPointCollectionResults.Result), ReadOnlyInflatedCollectionCollector.Empty, attributeSupportData.Result, context.Collectors.WindowsRuntimeTypeWithNames.Complete(), windowsRuntimeData.Result, context.Collectors.CCWMarshallingFunctions.Complete(), genericSharingAnalysisResults, null, analyticsData.Result);
		}
	}

	protected override AssemblyConversionResults.PrimaryWritePhase PrimaryWritePhase(GlobalFullyForkedContext context)
	{
		ReadOnlyPerAssemblyPendingResults<GenericContextCollection> pendingGenericContextCollections;
		using (IPhaseWorkScheduler<GlobalWriteContext> scheduler = CreateHackedScheduler(context.GlobalWriteContext, context.GlobalSchedulingContext))
		{
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteAssemblies"))
			{
				new WriteAssemblies().Schedule(scheduler, _assemblies);
			}
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteDriver"))
			{
				if (_isEntryAssembly)
				{
					new WriteExecutableDriver(_assemblies.Single()).Schedule(scheduler);
				}
			}
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteWindowsRuntimeFactories"))
			{
				new WriteWindowsRuntimeFactories().Schedule(scheduler);
			}
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteComCallableWrappers"))
			{
				new WriteComCallableWrappers().Schedule(scheduler);
			}
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteProjectedInterfacesByCCWs"))
			{
				new WriteProjectedInterfacesByComCallableWrappers().Schedule(scheduler);
			}
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterCollectGenericContextMetadata"))
			{
				pendingGenericContextCollections = new CollectGenericContextMetadata().Schedule(scheduler, _assemblies);
			}
		}
		using (context.Services.TinyProfiler.Section("Build Results"))
		{
			return new AssemblyConversionResults.PrimaryWritePhase(context.Collectors.GenericMethodCollector.Complete(), context.Collectors.Methods.Complete(), context.Collectors.TypeCollector.Complete(), context.Collectors.ReversePInvokeWrappers.Complete(), context.Collectors.TypeMarshallingFunctions.Complete(), context.Collectors.WrappersForDelegateFromManagedToNative.Complete(), context.Collectors.InteropGuids.Complete(), context.Collectors.MetadataUsage.Complete(), pendingGenericContextCollections.Result, context.Collectors.Symbols.Complete(), context.Collectors.MatchedAssemblyMethodSourceFiles.Complete());
		}
	}

	protected override AssemblyConversionResults.SecondaryCollectionPhase SecondaryCollectionPhase(GlobalFullyForkedContext context)
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
		using (IPhaseWorkScheduler<GlobalSecondaryCollectionContext> scheduler = CreateHackedScheduler(context.GlobalSecondaryCollectionContext, context.GlobalSchedulingContext))
		{
			pendingSortedMetadata = new SortMetadata().Schedule(scheduler);
			pendingInvokerCollectorResults = new CollectInvokers().Schedule(scheduler, _assemblies, scheduler.ContextForMainThread.Results.PrimaryWrite.GenericMethods.UnsortedKeys);
			pendingMethodTables = new CollectMethodTables().Schedule(scheduler);
			pendingInteropDataTable = new CollectInteropTable().Schedule(scheduler);
			pendingGenericInstanceCollection = new CollectGenericInstances().Schedule(scheduler);
			pendingFieldReferenceTable = new CollectFieldReferences().Schedule(scheduler);
			pendingStringLiteralTable = new CollectStringLiterals().Schedule(scheduler);
			pendingGenericMethodPointerNameTable = new CollectGenericMethodPointerNames().Schedule(scheduler, scheduler.SchedulingContext.Results.PrimaryWrite.GenericMethods.SortedKeys);
			pendingMethodPointerNameTable = new CollectMethodPointerNames().Schedule(scheduler, _assemblies);
		}
		using (context.Services.TinyProfiler.Section("Build Results"))
		{
			return new AssemblyConversionResults.SecondaryCollectionPhase(pendingInvokerCollectorResults?.Result, pendingMethodTables?.Result, pendingInteropDataTable?.Result, pendingFieldReferenceTable?.Result, pendingStringLiteralTable?.Result, pendingGenericInstanceCollection?.Result, pendingSortedMetadata?.Result, pendingMethodPointerNameTable?.Result, pendingGenericMethodPointerNameTable?.Result);
		}
	}

	protected override AssemblyConversionResults.SecondaryWritePhasePart1 SecondaryWritePhasePart1(GlobalFullyForkedContext context)
	{
		if (context.GlobalWriteContext.Parameters.EnableDebugger)
		{
			using IPhaseWorkScheduler<GlobalWriteContext> scheduler = CreateHackedScheduler(context.GlobalWriteContext, context.GlobalSchedulingContext);
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteDebuggerTables"))
			{
				new WriteDebuggerTables().Schedule(scheduler, _assemblies);
			}
		}
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
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteCodeMetadata"))
			{
				new WriteFullPerAssemblyCodeMetadata(MetadataUtils.RegistrationTableName(context.CreateReadOnlyContext()), CodeRegistrationWriter.CodeRegistrationTableName(context.CreateReadOnlyContext())).Schedule(scheduler, _assemblies);
			}
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteMetadata"))
			{
				new WriteGlobalMetadata().Schedule(scheduler);
			}
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteCodeRegistration"))
			{
				new WriteFullPerAssemblyCodeRegistration().Schedule(scheduler);
			}
			new WriteMethodMap(_assemblies).Schedule(scheduler, Array.Empty<ReadOnlyMethodPointerNameEntry>().AsReadOnly());
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
