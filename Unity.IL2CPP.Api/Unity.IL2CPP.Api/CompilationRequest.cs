using System;
using Unity.Api.Attributes;

namespace Unity.IL2CPP.Api;

[ContainsOptions]
public class CompilationRequest
{
	[HideFromHelp]
	public string Platform;

	[HideFromHelp]
	public string Architecture;

	[HelpDetails("Path to output the compiled binary", null)]
	[IsPath(PathKind.ExplicitlyHandledInBuildCode)]
	public string Outputpath;

	[HelpDetails("il2cpp will not use it's own baselib", null)]
	public bool DontDeployBaselib;

	[HelpDetails("Enables verbose output from tools involved in building", null)]
	public bool Verbose;

	[HideFromHelp]
	[IsPath(PathKind.NeitherInputNorOutput)]
	public string ToolChainPath;

	[HelpDetails("Forces a rebuild", null)]
	public bool Forcerebuild;

	[HelpDetails("Disable bee builder and use old buildcode", null)]
	public bool DisableBeeBuilder;

	[HelpDetails("Links il2cpp as library to the executable", null)]
	public bool Libil2cppStatic;

	[HelpDetails("Disable lumping for the runtime library", null)]
	public bool DisableRuntimeLumping;

	[HelpDetails("Cache directory to use when building libil2cpp as dynamic link library", null)]
	[IsPath(PathKind.NeitherInputNorOutput)]
	public string Libil2cppCacheDirectory;

	[HideFromHelp]
	public bool IncludeFileNamesInHashes;

	[HideFromHelp]
	public bool UseDependenciesToolChain;

	[HelpDetails("The number of jobs bee should use.  Defaults to the same as --jobs", null)]
	public int BeeJobs = -1;

	[HideFromHelp]
	public bool SetEnvironmentVariables;

	[HideFromHelp]
	[IsPath(PathKind.DirectoryWithInputFiles)]
	public string BaselibDirectory;

	[HideFromHelp]
	public bool AvoidDynamicLibraryCopy;

	[HideFromHelp]
	[IsPath(PathKind.NeitherInputNorOutput)]
	public string SysrootPath;

	[HelpDetails("Defines for generated C++ code compilation", null)]
	public string[] AdditionalDefines = Array.Empty<string>();

	[HelpDetails("One or more additional libraries to link to generated code", null)]
	[IsPath(PathKind.InputFile)]
	public string[] AdditionalLibraries = Array.Empty<string>();

	[HelpDetails("One or more additional include directories", "path")]
	[IsPath(PathKind.NeitherInputNorOutput)]
	public string[] AdditionalIncludeDirectories = Array.Empty<string>();

	[HelpDetails("One or more additional link directories", "path")]
	[IsPath(PathKind.NeitherInputNorOutput)]
	public string[] AdditionalLinkDirectories = Array.Empty<string>();

	[HideFromHelp]
	public CommandLogMode CommandLog;

	[HelpDetails("Path to an il2cpp plugin assembly", null)]
	[IsPath(PathKind.InputFile)]
	public string Plugin;

	[HelpDetails("Flag denoting if the compilation target is a simulator.", null)]
	public bool TargetIsSimulator;

	public CompilationSettings Settings { get; set; } = new CompilationSettings();

	public EmscriptenCompilationRequest Emscripten { get; set; } = new EmscriptenCompilationRequest();
}
