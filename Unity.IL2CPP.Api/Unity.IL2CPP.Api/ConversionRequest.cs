using System;
using Unity.Api.Attributes;

namespace Unity.IL2CPP.Api;

[ContainsOptions]
public sealed class ConversionRequest
{
	[IsPath(PathKind.InputFile)]
	[HelpDetails("One or more paths to assemblies to convert", "path")]
	public string[] Assembly = Array.Empty<string>();

	[IsPath(PathKind.DirectoryWithInputFiles)]
	[HelpDetails("One or more directories containing assemblies to convert", "path")]
	public string[] Directory = Array.Empty<string>();

	[Analytics("The number of extra types files that were included", AnalyticsDataType.ListCount)]
	[HelpDetails("One or more files containing a list of additonal generic instance types that should be included in the generated code", "path")]
	[IsPath(PathKind.InputFile)]
	public string[] ExtraTypesFile = Array.Empty<string>();

	[IsPath(PathKind.OutputDirectory)]
	[HelpDetails("The directory where generated C++ code is written", "path")]
	public string Generatedcppdir;

	[HelpDetails("The directory where symbol information will be written", "path")]
	[IsPath(PathKind.OutputDirectory)]
	public string SymbolsFolder;

	[HideFromHelp]
	[IsPath(PathKind.NeitherInputNorOutput)]
	public string ExecutableAssembliesFolderOnDevice;

	[HideFromHelp]
	public string EntryAssemblyName;

	[Analytics("The number of assemblies to emit debug information for", AnalyticsDataType.ListCount)]
	[HelpDetails("The name of an assembly (including .dll) to emit debug information for.  If this is provided, debug information from all others will be ignored.", null)]
	public string[] DebugAssemblyName = Array.Empty<string>();

	[HideFromHelp]
	public bool DebugEnableAttach;

	[HideFromHelp]
	[IsPath(PathKind.NeitherInputNorOutput)]
	public string DebugRiderInstallPath;

	[HideFromHelp]
	[IsPath(PathKind.NeitherInputNorOutput)]
	public string DebugSolutionPath;

	[HideFromHelp]
	public bool EnableDotMemory;

	[HideFromHelp]
	[IsPath(PathKind.NeitherInputNorOutput)]
	public string DotMemoryOutputPath;

	[HideFromHelp]
	public bool DotMemoryCollectAllocations;

	[HideFromHelp]
	public bool EnableDotTrace;

	[HideFromHelp]
	public string DotTraceProfilingType;

	[HideFromHelp]
	[IsPath(PathKind.NeitherInputNorOutput)]
	public string DotTraceOutputPath;

	[Analytics("The number of additional cpp files that were included", AnalyticsDataType.ListCount)]
	[HelpDetails("Additional C++ files to include", "path")]
	[IsPath(PathKind.InputFile)]
	public string[] AdditionalCpp = Array.Empty<string>();

	[HelpDetails("Enables collection of analytics", null)]
	public bool EnableAnalytics;

	public ConversionSettings Settings { get; set; } = new ConversionSettings();

	public ConversionStatistics Statistics { get; set; } = new ConversionStatistics();
}
