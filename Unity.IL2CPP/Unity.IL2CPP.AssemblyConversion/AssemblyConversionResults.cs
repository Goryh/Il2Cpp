using System;
using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.Api.Output.Analytics;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Attributes;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.GenericsCollection;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.AssemblyConversion;

public class AssemblyConversionResults : IGlobalContextPhaseResultsProvider
{
	public class InitializePhase
	{
		public readonly ReadOnlyCollection<AssemblyDefinition> AllAssembliesOrderedByDependency;

		public readonly ReadOnlyCollection<AssemblyDefinition> AllAssembliesOrderedByCostToProcess;

		public readonly AssemblyDefinition EntryAssembly;

		public readonly GenericLimitsResults GenericLimits;

		public InitializePhase(ReadOnlyCollection<AssemblyDefinition> allAssembliesOrderedByDependency, ReadOnlyCollection<AssemblyDefinition> allAssembliesOrderedByCostToProcess, AssemblyDefinition entryAssembly, GenericLimitsResults genericLimits)
		{
			AllAssembliesOrderedByDependency = allAssembliesOrderedByDependency;
			AllAssembliesOrderedByCostToProcess = allAssembliesOrderedByCostToProcess;
			EntryAssembly = entryAssembly;
			GenericLimits = genericLimits;
		}
	}

	public class SetupPhase
	{
		public readonly IRuntimeImplementedMethodWriterResults RuntimeImplementedMethodWriters;

		public SetupPhase(IRuntimeImplementedMethodWriterResults runtimeImplementedMethodWriters)
		{
			RuntimeImplementedMethodWriters = runtimeImplementedMethodWriters;
		}
	}

	public class PrimaryCollectionPhase
	{
		public readonly SequencePointProviderCollection SequencePoints;

		public readonly CatchPointCollectorCollection CatchPoints;

		public readonly ReadOnlyInflatedCollectionCollector Generics;

		public readonly ReadOnlyDictionary<AssemblyDefinition, ReadOnlyCollectedAttributeSupportData> AttributeSupportData;

		public readonly ReadOnlyCollection<Tuple<IIl2CppRuntimeType, string>> WindowsRuntimeTypeWithNames;

		public readonly ReadOnlyDictionary<AssemblyDefinition, CollectedWindowsRuntimeData> WindowsRuntimeData;

		public readonly ReadOnlyCollection<IIl2CppRuntimeType> CCWMarshalingFunctions;

		public readonly GenericSharingAnalysisResults GenericSharingAnalysis;

		public readonly IMetadataCollectionResults Metadata;

		public readonly ReadOnlyDictionary<AssemblyDefinition, AssemblyAnalyticsData> Analytics;

		public PrimaryCollectionPhase(SequencePointProviderCollection sequencePoints, CatchPointCollectorCollection catchPoints, ReadOnlyInflatedCollectionCollector generics, ReadOnlyDictionary<AssemblyDefinition, ReadOnlyCollectedAttributeSupportData> attributeSupportData, ReadOnlyCollection<Tuple<IIl2CppRuntimeType, string>> windowsRuntimeTypeWithNames, ReadOnlyDictionary<AssemblyDefinition, CollectedWindowsRuntimeData> windowsRuntimeData, ReadOnlyCollection<IIl2CppRuntimeType> ccwMarshalingFunctions, GenericSharingAnalysisResults genericSharingAnalysis, IMetadataCollectionResults metadataCollectionResults, ReadOnlyDictionary<AssemblyDefinition, AssemblyAnalyticsData> analytics)
		{
			SequencePoints = sequencePoints;
			CatchPoints = catchPoints;
			Generics = generics;
			AttributeSupportData = attributeSupportData;
			WindowsRuntimeTypeWithNames = windowsRuntimeTypeWithNames;
			WindowsRuntimeData = windowsRuntimeData;
			CCWMarshalingFunctions = ccwMarshalingFunctions;
			GenericSharingAnalysis = genericSharingAnalysis;
			Metadata = metadataCollectionResults;
			Analytics = analytics;
		}
	}

	public class PrimaryWritePhase
	{
		public readonly IMethodCollectorResults Methods;

		public readonly ITypeCollectorResults Types;

		public readonly IReversePInvokeWrapperCollectorResults ReversePInvokeWrappers;

		public readonly ReadOnlyCollection<IIl2CppRuntimeType> TypeMarshallingFunctions;

		public readonly ReadOnlyCollection<IIl2CppRuntimeType> WrappersForDelegateFromManagedToNative;

		public readonly ReadOnlyCollection<IIl2CppRuntimeType> InteropGuids;

		public readonly IMetadataUsageCollectorResults MetadataUsage;

		public readonly ReadOnlyDictionary<AssemblyDefinition, GenericContextCollection> GenericContextCollections;

		public readonly IGenericMethodCollectorResults GenericMethods;

		public readonly ISymbolsCollectorResults Symbols;

		public readonly ReadOnlyCollection<NPath> MatchedAssemblyMethodSourceFiles;

		public PrimaryWritePhase(IGenericMethodCollectorResults genericMethods, IMethodCollectorResults methods, ITypeCollectorResults typeCollectorResults, IReversePInvokeWrapperCollectorResults reversePInvokeWrappers, ReadOnlyCollection<IIl2CppRuntimeType> typeMarshallingFunctions, ReadOnlyCollection<IIl2CppRuntimeType> wrappersForDelegateFromManagedToNative, ReadOnlyCollection<IIl2CppRuntimeType> interopGuids, IMetadataUsageCollectorResults metadataUsageCollectorResults, ReadOnlyDictionary<AssemblyDefinition, GenericContextCollection> genericContextCollections, ISymbolsCollectorResults symbols, ReadOnlyCollection<NPath> matchedAssemblyMethodSourceFiles)
		{
			Methods = methods;
			Types = typeCollectorResults;
			ReversePInvokeWrappers = reversePInvokeWrappers;
			TypeMarshallingFunctions = typeMarshallingFunctions;
			WrappersForDelegateFromManagedToNative = wrappersForDelegateFromManagedToNative;
			InteropGuids = interopGuids;
			MetadataUsage = metadataUsageCollectorResults;
			GenericContextCollections = genericContextCollections;
			GenericMethods = genericMethods;
			Symbols = symbols;
			MatchedAssemblyMethodSourceFiles = matchedAssemblyMethodSourceFiles;
		}
	}

	public class SecondaryCollectionPhase
	{
		public readonly ReadOnlyInvokerCollection Invokers;

		public readonly ReadOnlyMethodTables MethodTables;

		public readonly ReadOnlyInteropTable InteropDataTable;

		public readonly ReadOnlyFieldReferenceTable FieldReferenceTable;

		public readonly ReadOnlyStringLiteralTable StringLiteralTable;

		public readonly ReadOnlyGenericInstanceTable GenericInstanceTable;

		public readonly ReadOnlySortedMetadata SortedMetadata;

		public readonly ReadOnlyDictionary<AssemblyDefinition, ReadOnlyMethodPointerNameTable> MethodPointerNameTable;

		public readonly ReadOnlyGenericMethodPointerNameTable GenericMethodPointerNameTable;

		public SecondaryCollectionPhase(ReadOnlyInvokerCollection invokers, ReadOnlyMethodTables methodTables, ReadOnlyInteropTable interopDataTable, ReadOnlyFieldReferenceTable fieldReferenceTable, ReadOnlyStringLiteralTable stringLiteralTable, ReadOnlyGenericInstanceTable genericInstanceTable, ReadOnlySortedMetadata sortedMetadata, ReadOnlyDictionary<AssemblyDefinition, ReadOnlyMethodPointerNameTable> methodPointerNameTable, ReadOnlyGenericMethodPointerNameTable genericMethodPointerNameTable)
		{
			Invokers = invokers;
			MethodTables = methodTables;
			InteropDataTable = interopDataTable;
			FieldReferenceTable = fieldReferenceTable;
			StringLiteralTable = stringLiteralTable;
			GenericInstanceTable = genericInstanceTable;
			SortedMetadata = sortedMetadata;
			MethodPointerNameTable = methodPointerNameTable;
			GenericMethodPointerNameTable = genericMethodPointerNameTable;
		}
	}

	public class SecondaryWritePhasePart1
	{
		public readonly IIndirectCallCollectorResults IndirectCalls;

		public SecondaryWritePhasePart1(IIndirectCallCollectorResults indirectCalls)
		{
			IndirectCalls = indirectCalls;
		}
	}

	public class SecondaryWritePhasePart3
	{
		public readonly UnresolvedIndirectCallsTableInfo UnresolvedIndirectCallsTableInfo;

		public SecondaryWritePhasePart3(UnresolvedIndirectCallsTableInfo unresolvedIndirectCallsTableInfo)
		{
			UnresolvedIndirectCallsTableInfo = unresolvedIndirectCallsTableInfo;
		}
	}

	public class SecondaryWritePhase
	{
		public readonly IFileResults FilesWritten;

		public SecondaryWritePhase(IFileResults filesWritten)
		{
			FilesWritten = filesWritten;
		}
	}

	public class CompletionPhase
	{
		public readonly IStatsResults Stats;

		public readonly ReadOnlyCollection<string> LoggedMessages;

		public readonly Il2CppDataTable AnalyticsTable;

		public CompletionPhase(IStatsResults statsResults, ReadOnlyCollection<string> loggedMessages, Il2CppDataTable analyticsTable)
		{
			Stats = statsResults;
			LoggedMessages = loggedMessages;
			AnalyticsTable = analyticsTable;
		}
	}

	private PrimaryCollectionPhase _primaryCollectionPhase;

	private PrimaryWritePhase _primaryWritePhase;

	private SecondaryCollectionPhase _secondaryCollection;

	private SecondaryWritePhasePart1 _secondaryWritePart1;

	private SecondaryWritePhasePart3 _secondaryWritePart3;

	private SecondaryWritePhase _secondaryWritePhase;

	private CompletionPhase _completionPhase;

	private SetupPhase _setupPhase;

	private InitializePhase _initializePhase;

	public InitializePhase Initialize
	{
		get
		{
			if (_initializePhase == null)
			{
				throw new InvalidOperationException("This information is not available until after the InitializePhase is complete");
			}
			return _initializePhase;
		}
	}

	public SetupPhase Setup
	{
		get
		{
			if (_setupPhase == null)
			{
				throw new InvalidOperationException("This information is not available until after the SetupPhase is complete");
			}
			return _setupPhase;
		}
	}

	public PrimaryCollectionPhase PrimaryCollection
	{
		get
		{
			if (_primaryCollectionPhase == null)
			{
				throw new InvalidOperationException("This information is not available until after the PrimaryCollectionPhase is complete");
			}
			return _primaryCollectionPhase;
		}
	}

	public PrimaryWritePhase PrimaryWrite
	{
		get
		{
			if (_primaryWritePhase == null)
			{
				throw new InvalidOperationException("This information is not available until after the PrimaryWritePhase is complete");
			}
			return _primaryWritePhase;
		}
	}

	public SecondaryCollectionPhase SecondaryCollection
	{
		get
		{
			if (_secondaryCollection == null)
			{
				throw new InvalidOperationException("This information is not available until after the SecondaryCollectionPhase is complete");
			}
			return _secondaryCollection;
		}
	}

	public SecondaryWritePhasePart1 SecondaryWritePart1
	{
		get
		{
			if (_secondaryWritePart1 == null)
			{
				throw new InvalidOperationException("This information is not available until after the SecondaryWritePhasePart1 is complete");
			}
			return _secondaryWritePart1;
		}
	}

	public SecondaryWritePhasePart3 SecondaryWritePart3
	{
		get
		{
			if (_secondaryWritePart3 == null)
			{
				throw new InvalidOperationException("This information is not available until after the SecondaryWritePhasePart3 is complete");
			}
			return _secondaryWritePart3;
		}
	}

	public SecondaryWritePhase SecondaryWrite
	{
		get
		{
			if (_secondaryWritePhase == null)
			{
				throw new InvalidOperationException("This information is not available until after the SecondaryWritePhase is complete");
			}
			return _secondaryWritePhase;
		}
	}

	public CompletionPhase Completion
	{
		get
		{
			if (_completionPhase == null)
			{
				throw new InvalidOperationException("This information is not available until after the CompletionPhase is complete");
			}
			return _completionPhase;
		}
	}

	public void SetPrimaryCollectionResults(PrimaryCollectionPhase results)
	{
		_primaryCollectionPhase = results;
	}

	public void SetPrimaryWritePhaseResults(PrimaryWritePhase results)
	{
		_primaryWritePhase = results;
	}

	public void SetSecondaryCollectionPhaseResults(SecondaryCollectionPhase results)
	{
		_secondaryCollection = results;
	}

	public void SetSecondaryWritePhasePart1Results(SecondaryWritePhasePart1 results)
	{
		_secondaryWritePart1 = results;
	}

	public void SetSecondaryWritePhasePart3Results(SecondaryWritePhasePart3 results)
	{
		_secondaryWritePart3 = results;
	}

	public void SetSecondaryWritePhaseResults(SecondaryWritePhase results)
	{
		_secondaryWritePhase = results;
	}

	public void SetCompletionPhaseResults(CompletionPhase results)
	{
		_completionPhase = results;
	}

	public void SetSetupPhaseResults(SetupPhase results)
	{
		_setupPhase = results;
	}

	public void SetInitializePhaseResults(InitializePhase results)
	{
		_initializePhase = results;
	}
}
