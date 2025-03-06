using System;
using Unity.Api.Attributes;

namespace Unity.IL2CPP.Api;

[ContainsOptions]
public class SettingsForConversionAndCompilation
{
	[HideFromHelp]
	public string Dotnetprofile = "UnityAot";

	[Analytics("Whether or not the this is a development build")]
	[HideFromHelp]
	public bool DevelopmentMode;

	[Analytics("Whether or not support for the managed debugger was enabled")]
	[HideFromHelp]
	public bool EnableDebugger;

	[Analytics("Whether or not generating a usym file was enabled")]
	[HelpDetails("Generate a symbol file for stacktrace line numbers", null)]
	public bool GenerateUsymFile;

	[HideFromHelp]
	[IsPath(PathKind.InputFile)]
	public string UsymtoolPath;

	[HideFromHelp]
	public bool DebuggerOff;

	[HideFromHelp]
	public bool WriteBarrierValidation;

	[HelpDetails("Enable code to allow the runtime to be shutdown and reloaded (this has code size and runtime performance impact).", null)]
	public bool EnableReload;

	[HideFromHelp]
	public bool GoogleBenchmark;

	[HelpDetails("A directory to use for caching compilation related files", "path")]
	[IsPath(PathKind.NeitherInputNorOutput)]
	public string Cachedirectory;

	[HelpDetails("Enable generation of a profiler report", null)]
	public bool ProfilerReport;

	[IsPath(PathKind.OutputFile)]
	[HelpDetails("The location where to write the profiler output", null)]
	public string ProfilerOutputFile;

	[HideFromHelp]
	public bool ProfilerUseTraceEvents;

	[HideFromHelp]
	public bool PrintCommandLine;

	[HideFromHelp]
	public string ExternalLibIl2Cpp;

	[HideFromHelp]
	public bool StaticLibIl2Cpp;

	[IsPath(PathKind.OutputDirectory)]
	[HelpDetails("The directory where non-source code data will be written", "path")]
	public string DataFolder;

	[Analytics("The number of jobs that were requested.  Defaults to the processor count")]
	[OptionAlias("j")]
	[HelpDetails("The number of cores to use during conversion and compilation.  Defaults to processor count", null)]
	public int Jobs = Environment.ProcessorCount;
}
