using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Providers;

public interface ITinyProfilerProvider
{
	ITinyProfilerService TinyProfiler { get; }
}
