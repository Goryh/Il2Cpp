using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Symbols;

namespace Unity.IL2CPP.Contexts.Forking.Providers;

public interface IUnrestrictedContextCollectorProvider
{
	MetadataUsageCollectorComponent MetadataUsage { get; }

	MethodCollectorComponent Methods { get; }

	SymbolsCollector Symbols { get; }

	IndirectCallCollectorComponent IndirectCalls { get; }

	TypeCollectorComponent TypeCollector { get; }

	GenericMethodCollectorComponent GenericMethodCollector { get; }

	RuntimeImplementedMethodWriterCollectorComponent RuntimeImplementedMethodWriterCollector { get; }

	StatsComponent Stats { get; }

	MatchedAssemblyMethodSourceFilesComponent MatchedAssemblyMethodSourceFiles { get; }

	ReversePInvokeWrapperComponent ReversePInvokeWrappers { get; }

	WindowsRuntimeTypeWithNameComponent WindowsRuntimeTypeWithNames { get; }

	CCWMarshallingFunctionComponent CCWMarshallingFunctions { get; }

	InteropGuidComponent InteropGuids { get; }

	TypeMarshallingFunctionsComponent TypeMarshallingFunctions { get; }

	WrapperForDelegateFromManagedToNativeComponent WrappersForDelegateFromManagedToNative { get; }
}
