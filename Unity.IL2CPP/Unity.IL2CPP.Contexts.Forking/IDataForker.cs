using System;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.Symbols;

namespace Unity.IL2CPP.Contexts.Forking;

public interface IDataForker<TContext>
{
	TContext CreateForkedContext();

	Action ForkMethods(MethodCollectorComponent component, in ForkingData data);

	Action ForkVirtualCalls(IndirectCallCollectorComponent component, in ForkingData data);

	Action ForkSymbols(SymbolsCollector component, in ForkingData data);

	Action ForkMetadataUsage(MetadataUsageCollectorComponent component, in ForkingData data);

	Action ForkStats(StatsComponent component, in ForkingData data);

	Action ForkTypes(TypeCollectorComponent component, in ForkingData data);

	Action ForkGenericMethods(GenericMethodCollectorComponent component, in ForkingData data);

	Action ForkRuntimeImplementedMethodWriters(RuntimeImplementedMethodWriterCollectorComponent component, in ForkingData data);

	Action ForkVTable(VTableBuilderComponent component, in ForkingData data);

	Action ForkTypeFactory(TypeFactoryComponent component, in ForkingData data);

	Action ForkGuidProvider(GuidProviderComponent component, in ForkingData data);

	Action ForkTypeProvider(TypeProviderComponent component, in ForkingData data);

	Action ForkFactory(ObjectFactoryComponent component, in ForkingData data);

	Action ForkWindowsRuntime(WindowsRuntimeProjectionsComponent component, in ForkingData data);

	Action ForkICallMapping(ICallMappingComponent component, in ForkingData data);

	Action ForkNaming(NamingComponent component, in ForkingData data);

	Action ForkSourceAnnotationWriter(SourceAnnotationWriterComponent component, in ForkingData data);

	Action ForkErrorInformation(ErrorInformationComponent component, in ForkingData data);

	Action ForkWorkers(ImmediateSchedulerComponent component, in ForkingData data);

	Action ForkMatchedAssemblyMethodSourceFiles(MatchedAssemblyMethodSourceFilesComponent component, in ForkingData data);

	Action ForkReversePInvokeWrappers(ReversePInvokeWrapperComponent component, in ForkingData data);

	Action ForkWindowsRuntimeTypeWithNames(WindowsRuntimeTypeWithNameComponent component, in ForkingData data);

	Action ForkCCWritingFunctions(CCWMarshallingFunctionComponent component, in ForkingData data);

	Action ForkInteropGuids(InteropGuidComponent component, in ForkingData data);

	Action ForkTypeMarshallingFunctions(TypeMarshallingFunctionsComponent component, in ForkingData data);

	Action ForkWrappersForDelegateFromManagedToNative(WrapperForDelegateFromManagedToNativeComponent component, in ForkingData data);

	Action ForkPathFactory(PathFactoryComponent component, in ForkingData data);

	Action ForkContextScopeService(ContextScopeServiceComponent component, in ForkingData data);

	Action ForkDiagnostics(DiagnosticsComponent component, in ForkingData data);

	Action ForkMessageLogger(MessageLoggerComponent component, in ForkingData data);

	Action ForkTinyProfiler(TinyProfilerComponent component, in ForkingData data);
}
