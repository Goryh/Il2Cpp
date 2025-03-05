using System;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Forking.Providers;

namespace Unity.IL2CPP.Contexts.Forking;

public static class ForkingRegistration
{
	public static void SetupMergeEntries<TContext>(IUnrestrictedContextDataProvider context, Action<object, Func<IDataForker<TContext>, ForkingData, Action>> registerCollector, ReadOnlyCollection<OverrideObjects> overrideObjects)
	{
		registerCollector(context.Collectors.MetadataUsage, (IDataForker<TContext> provider, ForkingData data) => provider.ForkMetadataUsage(context.Collectors.MetadataUsage, in data));
		registerCollector(context.Collectors.Methods, (IDataForker<TContext> provider, ForkingData data) => provider.ForkMethods(context.Collectors.Methods, in data));
		registerCollector(context.Collectors.Symbols, (IDataForker<TContext> provider, ForkingData data) => provider.ForkSymbols(context.Collectors.Symbols, in data));
		registerCollector(context.Collectors.IndirectCalls, (IDataForker<TContext> provider, ForkingData data) => provider.ForkVirtualCalls(context.Collectors.IndirectCalls, in data));
		registerCollector(context.Collectors.TypeCollector, (IDataForker<TContext> provider, ForkingData data) => provider.ForkTypes(context.Collectors.TypeCollector, in data));
		registerCollector(context.Collectors.GenericMethodCollector, (IDataForker<TContext> provider, ForkingData data) => provider.ForkGenericMethods(context.Collectors.GenericMethodCollector, in data));
		registerCollector(context.Collectors.RuntimeImplementedMethodWriterCollector, (IDataForker<TContext> provider, ForkingData data) => provider.ForkRuntimeImplementedMethodWriters(context.Collectors.RuntimeImplementedMethodWriterCollector, in data));
		registerCollector(context.Collectors.Stats, (IDataForker<TContext> provider, ForkingData data) => provider.ForkStats(context.Collectors.Stats, in data));
		registerCollector(context.Collectors.MatchedAssemblyMethodSourceFiles, (IDataForker<TContext> provider, ForkingData data) => provider.ForkMatchedAssemblyMethodSourceFiles(context.Collectors.MatchedAssemblyMethodSourceFiles, in data));
		registerCollector(context.Collectors.ReversePInvokeWrappers, (IDataForker<TContext> provider, ForkingData data) => provider.ForkReversePInvokeWrappers(context.Collectors.ReversePInvokeWrappers, in data));
		registerCollector(context.Collectors.WindowsRuntimeTypeWithNames, (IDataForker<TContext> provider, ForkingData data) => provider.ForkWindowsRuntimeTypeWithNames(context.Collectors.WindowsRuntimeTypeWithNames, in data));
		registerCollector(context.Collectors.CCWMarshallingFunctions, (IDataForker<TContext> provider, ForkingData data) => provider.ForkCCWritingFunctions(context.Collectors.CCWMarshallingFunctions, in data));
		registerCollector(context.Collectors.InteropGuids, (IDataForker<TContext> provider, ForkingData data) => provider.ForkInteropGuids(context.Collectors.InteropGuids, in data));
		registerCollector(context.Collectors.TypeMarshallingFunctions, (IDataForker<TContext> provider, ForkingData data) => provider.ForkTypeMarshallingFunctions(context.Collectors.TypeMarshallingFunctions, in data));
		registerCollector(context.Collectors.WrappersForDelegateFromManagedToNative, (IDataForker<TContext> provider, ForkingData data) => provider.ForkWrappersForDelegateFromManagedToNative(context.Collectors.WrappersForDelegateFromManagedToNative, in data));
		registerCollector(context.Services.Factory, (IDataForker<TContext> provider, ForkingData data) => provider.ForkFactory(context.Services.Factory, in data));
		registerCollector(context.Services.GuidProvider, (IDataForker<TContext> provider, ForkingData data) => provider.ForkGuidProvider(context.Services.GuidProvider, in data));
		registerCollector(context.Services.TypeProvider, (IDataForker<TContext> provider, ForkingData data) => provider.ForkTypeProvider(context.Services.TypeProvider, in data));
		registerCollector(context.Services.ICallMapping, (IDataForker<TContext> provider, ForkingData data) => provider.ForkICallMapping(context.Services.ICallMapping, in data));
		registerCollector(context.Services.WindowsRuntimeProjections, (IDataForker<TContext> provider, ForkingData data) => provider.ForkWindowsRuntime(context.Services.WindowsRuntimeProjections, in data));
		registerCollector(context.Services.ContextScope, (IDataForker<TContext> provider, ForkingData data) => provider.ForkContextScopeService(Pick(context.Services.ContextScope, overrideObjects?[data.Index]?.ContextScope), in data));
		registerCollector(context.Services.TinyProfiler, (IDataForker<TContext> provider, ForkingData data) => provider.ForkTinyProfiler(context.Services.TinyProfiler, in data));
		registerCollector(context.StatefulServices.Naming, (IDataForker<TContext> provider, ForkingData data) => provider.ForkNaming(context.StatefulServices.Naming, in data));
		registerCollector(context.StatefulServices.ErrorInformation, (IDataForker<TContext> provider, ForkingData data) => provider.ForkErrorInformation(context.StatefulServices.ErrorInformation, in data));
		registerCollector(context.StatefulServices.SourceAnnotationWriter, (IDataForker<TContext> provider, ForkingData data) => provider.ForkSourceAnnotationWriter(context.StatefulServices.SourceAnnotationWriter, in data));
		registerCollector(context.StatefulServices.Scheduler, (IDataForker<TContext> provider, ForkingData data) => provider.ForkWorkers(Pick(context.StatefulServices.Scheduler, overrideObjects?[data.Index]?.Workers), in data));
		registerCollector(context.StatefulServices.Diagnostics, (IDataForker<TContext> provider, ForkingData data) => provider.ForkDiagnostics(context.StatefulServices.Diagnostics, in data));
		registerCollector(context.StatefulServices.MessageLogger, (IDataForker<TContext> provider, ForkingData data) => provider.ForkMessageLogger(context.StatefulServices.MessageLogger, in data));
		registerCollector(context.StatefulServices.VTableBuilder, (IDataForker<TContext> provider, ForkingData data) => provider.ForkVTable(context.StatefulServices.VTableBuilder, in data));
		registerCollector(context.StatefulServices.TypeFactory, (IDataForker<TContext> provider, ForkingData data) => provider.ForkTypeFactory(context.StatefulServices.TypeFactory, in data));
		registerCollector(context.StatefulServices.PathFactory, (IDataForker<TContext> provider, ForkingData data) => provider.ForkPathFactory(Pick(context.StatefulServices.PathFactory, overrideObjects?[data.Index]?.PathFactory), in data));
	}

	private static T Pick<T>(T @default, T @override)
	{
		if (@override != null)
		{
			return @override;
		}
		return @default;
	}
}
