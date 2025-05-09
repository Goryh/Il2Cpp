using Unity.IL2CPP.Contexts.Collectors;

namespace Unity.IL2CPP.Contexts.Forking.Providers;

public interface IGlobalContextCollectorProvider
{
	IMethodCollector Methods { get; }

	IIndirectCallCollector IndirectCalls { get; }

	ISymbolsCollector Symbols { get; }

	IMetadataUsageCollectorWriterService MetadataUsage { get; }

	IStatsWriterService Stats { get; }

	ITypeCollector Types { get; }

	IGenericMethodCollector GenericMethods { get; }

	IRuntimeImplementedMethodWriterCollector RuntimeImplementedMethodWriters { get; }

	IMatchedAssemblyMethodSourceFilesCollector MatchedAssemblyMethodSourceFiles { get; }

	IReversePInvokeWrapperCollector ReversePInvokeWrappers { get; }

	IWindowsRuntimeTypeWithNameCollector WindowsRuntimeTypeWithNames { get; }

	ICCWMarshallingFunctionCollector CCWMarshallingFunctions { get; }

	IInteropGuidCollector InteropGuids { get; }

	ITypeMarshallingFunctionsCollector TypeMarshallingFunctions { get; }

	IWrapperForDelegateFromManagedToNativeCollector WrappersForDelegateFromManagedToNative { get; }
}
