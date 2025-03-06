namespace Unity.IL2CPP.Api;

public class EnvironmentData
{
	public string Il2CppDistributionDirectory;

	public string Il2CppInvocationString;

	public string BaselibDirectory;

	public string Il2CppDepsDirectory;

	public string LinkerInvocationString;

	public string DirectoryHoldingIL2CPP;

	public string DirectoryHoldingUnityLinker;

	public string PlatformDirectory;

	public string BuildLogicAssemblyPath;

	public string MonoSourcesDirectory { get; set; } = "external/mono/mono";

	public string LibIL2CPPDirectory { get; set; } = "libil2cpp";

	public string BdwgcDirectory { get; set; } = "external/bdwgc";
}
