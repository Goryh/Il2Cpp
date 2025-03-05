using System;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.Symbols;

namespace Unity.IL2CPP.Contexts.Forking.Steps;

public abstract class BaseDataForker<TContext> : IDataForker<TContext>
{
	protected delegate void ReadWrite<TWrite, TRead, TFull>(in ForkingData data, out TWrite write, out TRead read, out TFull full);

	protected delegate void ReadOnly<TRead, TFull>(in ForkingData data, out object write, out TRead read, out TFull full);

	protected delegate void WriteOnly<TWrite, TFull>(in ForkingData data, out TWrite write, out object read, out TFull full);

	protected readonly ForkedDataContainer _container;

	protected readonly ForkedDataProvider _forkedProvider;

	protected BaseDataForker(IUnrestrictedContextDataProvider context)
		: this(new ForkedDataProvider(context, new ForkedDataContainer()))
	{
	}

	protected BaseDataForker(ForkedDataProvider forkedProvider)
	{
		_container = forkedProvider.Container;
		_forkedProvider = forkedProvider;
	}

	public abstract TContext CreateForkedContext();

	private Action ForkAndMergeReadWrite<TWrite, TRead, TFull>(in ForkingData data, ReadWrite<TWrite, TRead, TFull> forkable, Action<TFull> merge, out TWrite writer, out TRead reader, out TFull full)
	{
		forkable(in data, out writer, out reader, out full);
		if (writer == null)
		{
			throw new ArgumentNullException($"{forkable.GetType()} returned a null `{"writer"}` which is not allowed");
		}
		if (reader == null)
		{
			throw new ArgumentNullException($"{forkable.GetType()} returned a null `{"reader"}` which is not allowed");
		}
		if (full == null)
		{
			throw new ArgumentNullException($"{forkable.GetType()} returned a null `{"full"}` which is not allowed");
		}
		TFull tmpFull = full;
		return delegate
		{
			merge(tmpFull);
		};
	}

	private Action ForkAndMergeReadOnly<TRead, TFull>(in ForkingData data, ReadOnly<TRead, TFull> forkable, Action<TFull> merge, out TRead reader, out TFull full)
	{
		forkable(in data, out var _, out reader, out full);
		if (reader == null)
		{
			throw new ArgumentNullException($"{forkable.GetType()} returned a null `{"reader"}` which is not allowed");
		}
		if (full == null)
		{
			throw new ArgumentNullException($"{forkable.GetType()} returned a null `{"full"}` which is not allowed");
		}
		TFull tmpFull = full;
		return delegate
		{
			merge(tmpFull);
		};
	}

	private Action ForkAndMergeWriteOnly<TWrite, TFull>(in ForkingData data, WriteOnly<TWrite, TFull> forkable, Action<TFull> merge, out TWrite writer, out TFull full)
	{
		forkable(in data, out writer, out var _, out full);
		if (writer == null)
		{
			throw new ArgumentNullException($"{forkable.GetType()} returned a null `{"writer"}` which is not allowed");
		}
		if (full == null)
		{
			throw new ArgumentNullException($"{forkable.GetType()} returned a null `{"full"}` which is not allowed");
		}
		TFull tmpFull = full;
		return delegate
		{
			merge(tmpFull);
		};
	}

	protected abstract ReadWrite<TWrite, TRead, TFull> PickFork<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component);

	protected abstract WriteOnly<TWrite, TFull> PickFork<TWrite, TFull>(IForkableComponent<TWrite, object, TFull> component);

	protected abstract ReadOnly<TRead, TFull> PickFork<TRead, TFull>(IForkableComponent<object, TRead, TFull> component);

	protected abstract Action<TFull> PickMerge<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component);

	public Action ForkMethods(MethodCollectorComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorMethods, out _container.Methods);
	}

	public Action ForkVirtualCalls(IndirectCallCollectorComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorIndirectCalls, out _container.IndirectCalls);
	}

	public Action ForkSymbols(SymbolsCollector component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorSymbols, out _container.Symbols);
	}

	public Action ForkMetadataUsage(MetadataUsageCollectorComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorMetadataUsage, out _container.MetadataUsage);
	}

	public Action ForkStats(StatsComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorStats, out _container.Stats);
	}

	public Action ForkTypes(TypeCollectorComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorTypes, out _container.TypeCollector);
	}

	public Action ForkGenericMethods(GenericMethodCollectorComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorGenericMethods, out _container.GenericMethodCollector);
	}

	public Action ForkRuntimeImplementedMethodWriters(RuntimeImplementedMethodWriterCollectorComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorRuntimeImplementedMethodWriter, out _container.RuntimeImplementedMethodWriterCollector);
	}

	public Action ForkVTable(VTableBuilderComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.StatefulServicesVTable, out _container.VTableBuilder);
	}

	public Action ForkTypeFactory(TypeFactoryComponent component, in ForkingData data)
	{
		return ForkAndMergeReadOnly(in data, PickFork(component), PickMerge(component), out _container.StatefulServiceTypeFactory, out _container.TypeFactory);
	}

	public Action ForkGuidProvider(GuidProviderComponent component, in ForkingData data)
	{
		return ForkAndMergeReadOnly(in data, PickFork(component), PickMerge(component), out _container.ServicesGuidProvider, out _container.GuidProvider);
	}

	public Action ForkTypeProvider(TypeProviderComponent component, in ForkingData data)
	{
		return ForkAndMergeReadOnly(in data, PickFork(component), PickMerge(component), out _container.ServicesTypeProvider, out _container.TypeProvider);
	}

	public Action ForkFactory(ObjectFactoryComponent component, in ForkingData data)
	{
		return ForkAndMergeReadOnly(in data, PickFork(component), PickMerge(component), out _container.ServicesFactory, out _container.Factory);
	}

	public Action ForkWindowsRuntime(WindowsRuntimeProjectionsComponent component, in ForkingData data)
	{
		return ForkAndMergeReadOnly(in data, PickFork(component), PickMerge(component), out _container.ServicesWindowsRuntime, out _container.WindowsRuntimeProjections);
	}

	public Action ForkICallMapping(ICallMappingComponent component, in ForkingData data)
	{
		return ForkAndMergeReadOnly(in data, PickFork(component), PickMerge(component), out _container.ServicesICallMapping, out _container.ICallMapping);
	}

	public Action ForkNaming(NamingComponent component, in ForkingData data)
	{
		return ForkAndMergeReadOnly(in data, PickFork(component), PickMerge(component), out _container.StatefulServicesNaming, out _container.Naming);
	}

	public Action ForkSourceAnnotationWriter(SourceAnnotationWriterComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.StatefulServicesSourceAnnotationWriter, out _container.SourceAnnotationWriter);
	}

	public Action ForkErrorInformation(ErrorInformationComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.StatefulServicesErrorInformation, out _container.ErrorInformation);
	}

	public Action ForkWorkers(ImmediateSchedulerComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.StatefulServicesScheduler, out _container.Scheduler);
	}

	public Action ForkMatchedAssemblyMethodSourceFiles(MatchedAssemblyMethodSourceFilesComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorMatchedAssemblyMethodSourceFiles, out _container.MatchedAssemblyMethodSourceFiles);
	}

	public Action ForkReversePInvokeWrappers(ReversePInvokeWrapperComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorReversePInvokeWrappers, out _container.ReversePInvokeWrappers);
	}

	public Action ForkWindowsRuntimeTypeWithNames(WindowsRuntimeTypeWithNameComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorWindowsRuntimeTypeWithNames, out _container.WindowsRuntimeTypeWithNames);
	}

	public Action ForkCCWritingFunctions(CCWMarshallingFunctionComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorCcwMarshallingFunctions, out _container.CcwMarshallingFunctions);
	}

	public Action ForkInteropGuids(InteropGuidComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorInteropGuids, out _container.InteropGuids);
	}

	public Action ForkTypeMarshallingFunctions(TypeMarshallingFunctionsComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorTypeMarshallingFunctions, out _container.TypeMarshallingFunctions);
	}

	public Action ForkWrappersForDelegateFromManagedToNative(WrapperForDelegateFromManagedToNativeComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.CollectorWrappersForDelegateFromManagedToNative, out _container.WrappersForDelegateFromManagedToNative);
	}

	public Action ForkPathFactory(PathFactoryComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.StatefulServicesPathFactory, out _container.PathFactory);
	}

	public Action ForkContextScopeService(ContextScopeServiceComponent component, in ForkingData data)
	{
		return ForkAndMergeReadOnly(in data, PickFork(component), PickMerge(component), out _container.ServicesContextScopeService, out _container.ContextScope);
	}

	public Action ForkDiagnostics(DiagnosticsComponent component, in ForkingData data)
	{
		return ForkAndMergeReadOnly(in data, PickFork(component), PickMerge(component), out _container.StatefulServicesDiagnostics, out _container.Diagnostics);
	}

	public Action ForkMessageLogger(MessageLoggerComponent component, in ForkingData data)
	{
		return ForkAndMergeWriteOnly(in data, PickFork(component), PickMerge(component), out _container.StatefulServicesMessageLogger, out _container.MessageLogger);
	}

	public Action ForkTinyProfiler(TinyProfilerComponent component, in ForkingData data)
	{
		return ForkAndMergeReadOnly(in data, PickFork(component), PickMerge(component), out _container.ServicesTinyProfiler, out _container.TinyProfiler);
	}

	Action IDataForker<TContext>.ForkMethods(MethodCollectorComponent component, in ForkingData data)
	{
		return ForkMethods(component, in data);
	}

	Action IDataForker<TContext>.ForkVirtualCalls(IndirectCallCollectorComponent component, in ForkingData data)
	{
		return ForkVirtualCalls(component, in data);
	}

	Action IDataForker<TContext>.ForkSymbols(SymbolsCollector component, in ForkingData data)
	{
		return ForkSymbols(component, in data);
	}

	Action IDataForker<TContext>.ForkMetadataUsage(MetadataUsageCollectorComponent component, in ForkingData data)
	{
		return ForkMetadataUsage(component, in data);
	}

	Action IDataForker<TContext>.ForkStats(StatsComponent component, in ForkingData data)
	{
		return ForkStats(component, in data);
	}

	Action IDataForker<TContext>.ForkTypes(TypeCollectorComponent component, in ForkingData data)
	{
		return ForkTypes(component, in data);
	}

	Action IDataForker<TContext>.ForkGenericMethods(GenericMethodCollectorComponent component, in ForkingData data)
	{
		return ForkGenericMethods(component, in data);
	}

	Action IDataForker<TContext>.ForkRuntimeImplementedMethodWriters(RuntimeImplementedMethodWriterCollectorComponent component, in ForkingData data)
	{
		return ForkRuntimeImplementedMethodWriters(component, in data);
	}

	Action IDataForker<TContext>.ForkVTable(VTableBuilderComponent component, in ForkingData data)
	{
		return ForkVTable(component, in data);
	}

	Action IDataForker<TContext>.ForkTypeFactory(TypeFactoryComponent component, in ForkingData data)
	{
		return ForkTypeFactory(component, in data);
	}

	Action IDataForker<TContext>.ForkGuidProvider(GuidProviderComponent component, in ForkingData data)
	{
		return ForkGuidProvider(component, in data);
	}

	Action IDataForker<TContext>.ForkTypeProvider(TypeProviderComponent component, in ForkingData data)
	{
		return ForkTypeProvider(component, in data);
	}

	Action IDataForker<TContext>.ForkFactory(ObjectFactoryComponent component, in ForkingData data)
	{
		return ForkFactory(component, in data);
	}

	Action IDataForker<TContext>.ForkWindowsRuntime(WindowsRuntimeProjectionsComponent component, in ForkingData data)
	{
		return ForkWindowsRuntime(component, in data);
	}

	Action IDataForker<TContext>.ForkICallMapping(ICallMappingComponent component, in ForkingData data)
	{
		return ForkICallMapping(component, in data);
	}

	Action IDataForker<TContext>.ForkNaming(NamingComponent component, in ForkingData data)
	{
		return ForkNaming(component, in data);
	}

	Action IDataForker<TContext>.ForkSourceAnnotationWriter(SourceAnnotationWriterComponent component, in ForkingData data)
	{
		return ForkSourceAnnotationWriter(component, in data);
	}

	Action IDataForker<TContext>.ForkErrorInformation(ErrorInformationComponent component, in ForkingData data)
	{
		return ForkErrorInformation(component, in data);
	}

	Action IDataForker<TContext>.ForkWorkers(ImmediateSchedulerComponent component, in ForkingData data)
	{
		return ForkWorkers(component, in data);
	}

	Action IDataForker<TContext>.ForkMatchedAssemblyMethodSourceFiles(MatchedAssemblyMethodSourceFilesComponent component, in ForkingData data)
	{
		return ForkMatchedAssemblyMethodSourceFiles(component, in data);
	}

	Action IDataForker<TContext>.ForkReversePInvokeWrappers(ReversePInvokeWrapperComponent component, in ForkingData data)
	{
		return ForkReversePInvokeWrappers(component, in data);
	}

	Action IDataForker<TContext>.ForkWindowsRuntimeTypeWithNames(WindowsRuntimeTypeWithNameComponent component, in ForkingData data)
	{
		return ForkWindowsRuntimeTypeWithNames(component, in data);
	}

	Action IDataForker<TContext>.ForkCCWritingFunctions(CCWMarshallingFunctionComponent component, in ForkingData data)
	{
		return ForkCCWritingFunctions(component, in data);
	}

	Action IDataForker<TContext>.ForkInteropGuids(InteropGuidComponent component, in ForkingData data)
	{
		return ForkInteropGuids(component, in data);
	}

	Action IDataForker<TContext>.ForkTypeMarshallingFunctions(TypeMarshallingFunctionsComponent component, in ForkingData data)
	{
		return ForkTypeMarshallingFunctions(component, in data);
	}

	Action IDataForker<TContext>.ForkWrappersForDelegateFromManagedToNative(WrapperForDelegateFromManagedToNativeComponent component, in ForkingData data)
	{
		return ForkWrappersForDelegateFromManagedToNative(component, in data);
	}

	Action IDataForker<TContext>.ForkPathFactory(PathFactoryComponent component, in ForkingData data)
	{
		return ForkPathFactory(component, in data);
	}

	Action IDataForker<TContext>.ForkContextScopeService(ContextScopeServiceComponent component, in ForkingData data)
	{
		return ForkContextScopeService(component, in data);
	}

	Action IDataForker<TContext>.ForkDiagnostics(DiagnosticsComponent component, in ForkingData data)
	{
		return ForkDiagnostics(component, in data);
	}

	Action IDataForker<TContext>.ForkMessageLogger(MessageLoggerComponent component, in ForkingData data)
	{
		return ForkMessageLogger(component, in data);
	}

	Action IDataForker<TContext>.ForkTinyProfiler(TinyProfilerComponent component, in ForkingData data)
	{
		return ForkTinyProfiler(component, in data);
	}
}
