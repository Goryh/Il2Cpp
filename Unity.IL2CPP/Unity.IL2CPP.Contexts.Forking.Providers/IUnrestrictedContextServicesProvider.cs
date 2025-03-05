using Unity.IL2CPP.Contexts.Components;

namespace Unity.IL2CPP.Contexts.Forking.Providers;

public interface IUnrestrictedContextServicesProvider
{
	ICallMappingComponent ICallMapping { get; }

	GuidProviderComponent GuidProvider { get; }

	TypeProviderComponent TypeProvider { get; }

	WindowsRuntimeProjectionsComponent WindowsRuntimeProjections { get; }

	ObjectFactoryComponent Factory { get; }

	ContextScopeServiceComponent ContextScope { get; }

	TinyProfilerComponent TinyProfiler { get; }
}
