using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Forking.Providers;

public interface IGlobalContextServicesProvider
{
	IGuidProvider GuidProvider { get; }

	ITypeProviderService TypeProvider { get; }

	IObjectFactory Factory { get; }

	IWindowsRuntimeProjections WindowsRuntime { get; }

	IICallMappingService ICallMapping { get; }

	IContextScopeService ContextScope { get; }

	ITinyProfilerService TinyProfiler { get; }
}
