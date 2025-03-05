using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Symbols;

namespace Unity.IL2CPP.AssemblyConversion;

public class AssemblyConversionCollectors : IGlobalContextCollectorProvider, IGlobalContextResultsProvider, IUnrestrictedContextCollectorProvider
{
	public readonly MetadataUsageCollectorComponent MetadataUsage = new MetadataUsageCollectorComponent();

	public readonly MethodCollectorComponent Methods = new MethodCollectorComponent();

	public readonly SymbolsCollector Symbols = new SymbolsCollector();

	public readonly IndirectCallCollectorComponent IndirectCalls = new IndirectCallCollectorComponent();

	public readonly TypeCollectorComponent TypeCollector = new TypeCollectorComponent();

	public readonly ReversePInvokeWrapperComponent ReversePInvokeWrappers = new ReversePInvokeWrapperComponent();

	public readonly WindowsRuntimeTypeWithNameComponent WindowsRuntimeTypeWithNames = new WindowsRuntimeTypeWithNameComponent();

	public readonly CCWMarshallingFunctionComponent CCWMarshallingFunctions = new CCWMarshallingFunctionComponent();

	public readonly InteropGuidComponent InteropGuids = new InteropGuidComponent();

	public readonly TypeMarshallingFunctionsComponent TypeMarshallingFunctions = new TypeMarshallingFunctionsComponent();

	public readonly WrapperForDelegateFromManagedToNativeComponent WrappersForDelegateFromManagedToNative = new WrapperForDelegateFromManagedToNativeComponent();

	public readonly GenericMethodCollectorComponent GenericMethodCollector = new GenericMethodCollectorComponent();

	public readonly RuntimeImplementedMethodWriterCollectorComponent RuntimeImplementedMethodWriterCollector = new RuntimeImplementedMethodWriterCollectorComponent();

	public readonly StatsComponent Stats = new StatsComponent();

	public readonly MatchedAssemblyMethodSourceFilesComponent MatchedAssemblyMethodSourceFiles = new MatchedAssemblyMethodSourceFilesComponent();

	MetadataUsageCollectorComponent IUnrestrictedContextCollectorProvider.MetadataUsage => MetadataUsage;

	MethodCollectorComponent IUnrestrictedContextCollectorProvider.Methods => Methods;

	SymbolsCollector IUnrestrictedContextCollectorProvider.Symbols => Symbols;

	IndirectCallCollectorComponent IUnrestrictedContextCollectorProvider.IndirectCalls => IndirectCalls;

	TypeCollectorComponent IUnrestrictedContextCollectorProvider.TypeCollector => TypeCollector;

	GenericMethodCollectorComponent IUnrestrictedContextCollectorProvider.GenericMethodCollector => GenericMethodCollector;

	RuntimeImplementedMethodWriterCollectorComponent IUnrestrictedContextCollectorProvider.RuntimeImplementedMethodWriterCollector => RuntimeImplementedMethodWriterCollector;

	StatsComponent IUnrestrictedContextCollectorProvider.Stats => Stats;

	MatchedAssemblyMethodSourceFilesComponent IUnrestrictedContextCollectorProvider.MatchedAssemblyMethodSourceFiles => MatchedAssemblyMethodSourceFiles;

	ReversePInvokeWrapperComponent IUnrestrictedContextCollectorProvider.ReversePInvokeWrappers => ReversePInvokeWrappers;

	WindowsRuntimeTypeWithNameComponent IUnrestrictedContextCollectorProvider.WindowsRuntimeTypeWithNames => WindowsRuntimeTypeWithNames;

	CCWMarshallingFunctionComponent IUnrestrictedContextCollectorProvider.CCWMarshallingFunctions => CCWMarshallingFunctions;

	InteropGuidComponent IUnrestrictedContextCollectorProvider.InteropGuids => InteropGuids;

	TypeMarshallingFunctionsComponent IUnrestrictedContextCollectorProvider.TypeMarshallingFunctions => TypeMarshallingFunctions;

	WrapperForDelegateFromManagedToNativeComponent IUnrestrictedContextCollectorProvider.WrappersForDelegateFromManagedToNative => WrappersForDelegateFromManagedToNative;

	IInteropGuidCollector IGlobalContextCollectorProvider.InteropGuids => InteropGuids;

	ITypeMarshallingFunctionsCollector IGlobalContextCollectorProvider.TypeMarshallingFunctions => TypeMarshallingFunctions;

	IWrapperForDelegateFromManagedToNativeCollector IGlobalContextCollectorProvider.WrappersForDelegateFromManagedToNative => WrappersForDelegateFromManagedToNative;

	ICCWMarshallingFunctionCollector IGlobalContextCollectorProvider.CCWMarshallingFunctions => CCWMarshallingFunctions;

	IReversePInvokeWrapperCollector IGlobalContextCollectorProvider.ReversePInvokeWrappers => ReversePInvokeWrappers;

	IWindowsRuntimeTypeWithNameCollector IGlobalContextCollectorProvider.WindowsRuntimeTypeWithNames => WindowsRuntimeTypeWithNames;

	IMethodCollector IGlobalContextCollectorProvider.Methods => Methods;

	IIndirectCallCollector IGlobalContextCollectorProvider.IndirectCalls => IndirectCalls;

	ISymbolsCollector IGlobalContextCollectorProvider.Symbols => Symbols;

	IMetadataUsageCollectorWriterService IGlobalContextCollectorProvider.MetadataUsage => MetadataUsage;

	IStatsWriterService IGlobalContextCollectorProvider.Stats => Stats;

	ITypeCollector IGlobalContextCollectorProvider.Types => TypeCollector;

	IGenericMethodCollector IGlobalContextCollectorProvider.GenericMethods => GenericMethodCollector;

	IRuntimeImplementedMethodWriterCollector IGlobalContextCollectorProvider.RuntimeImplementedMethodWriters => RuntimeImplementedMethodWriterCollector;

	IMatchedAssemblyMethodSourceFilesCollector IGlobalContextCollectorProvider.MatchedAssemblyMethodSourceFiles => MatchedAssemblyMethodSourceFiles;
}
