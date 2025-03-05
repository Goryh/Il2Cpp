using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Services;
using Unity.TinyProfiler;

namespace Unity.IL2CPP.AssemblyConversion;

public class AssemblyConversionServices : IGlobalContextServicesProvider, IUnrestrictedContextServicesProvider
{
	public readonly ICallMappingComponent ICallMapping = new ICallMappingComponent();

	internal readonly GuidProviderComponent GuidProvider = new GuidProviderComponent();

	public readonly TypeProviderComponent TypeProvider = new TypeProviderComponent();

	public readonly DataModelComponent DataModel = new DataModelComponent();

	public readonly WindowsRuntimeProjectionsComponent WindowsRuntimeProjections = new WindowsRuntimeProjectionsComponent();

	public readonly ObjectFactoryComponent Factory;

	public readonly ContextScopeServiceComponent ContextScope = new ContextScopeServiceComponent();

	public readonly TinyProfilerComponent TinyProfiler;

	ICallMappingComponent IUnrestrictedContextServicesProvider.ICallMapping => ICallMapping;

	GuidProviderComponent IUnrestrictedContextServicesProvider.GuidProvider => GuidProvider;

	TypeProviderComponent IUnrestrictedContextServicesProvider.TypeProvider => TypeProvider;

	WindowsRuntimeProjectionsComponent IUnrestrictedContextServicesProvider.WindowsRuntimeProjections => WindowsRuntimeProjections;

	ObjectFactoryComponent IUnrestrictedContextServicesProvider.Factory => Factory;

	IGuidProvider IGlobalContextServicesProvider.GuidProvider => GuidProvider;

	ITypeProviderService IGlobalContextServicesProvider.TypeProvider => TypeProvider;

	IObjectFactory IGlobalContextServicesProvider.Factory => Factory;

	IWindowsRuntimeProjections IGlobalContextServicesProvider.WindowsRuntime => WindowsRuntimeProjections;

	IICallMappingService IGlobalContextServicesProvider.ICallMapping => ICallMapping;

	IContextScopeService IGlobalContextServicesProvider.ContextScope => ContextScope;

	TinyProfilerComponent IUnrestrictedContextServicesProvider.TinyProfiler => TinyProfiler;

	ITinyProfilerService IGlobalContextServicesProvider.TinyProfiler => TinyProfiler;

	ContextScopeServiceComponent IUnrestrictedContextServicesProvider.ContextScope => ContextScope;

	public AssemblyConversionServices(TinyProfiler2 tinyProfiler)
	{
		Factory = new ObjectFactoryComponent();
		TinyProfiler = new TinyProfilerComponent(tinyProfiler);
	}
}
