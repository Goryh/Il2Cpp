using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Api.Attributes;

namespace Unity.IL2CPP.Api;

[ContainsOptions]
public class CompilationSettings
{
	public string Identifier;

	[HideFromHelp]
	public int DebuggerPort;

	[HelpDetails("Enable incremental GC if n > 0, with a maximum time slice of n ms.", null)]
	public int IncrementalGCTimeSlice;

	[HelpDetails("Optional. Specifies path of IL2CPP data directory relative to deployed application working directory.", null)]
	public string RelativeDataPath = "Data";

	[HideFromHelp]
	public bool DontLinkCrt;

	[HideFromHelp]
	public bool TargetBitcode;

	[HideFromHelp]
	public bool EnableArmPacBti;

	[HelpDetails("The build configuration.  Debug|Release", null)]
	public BuildConfiguration Configuration;

	[HelpDetails("Enables warnings as errors for compiling generated C++ code", null)]
	public bool TreatWarningsAsErrors;

	[HelpDetails("Additional flags to pass to the C++ compiler", null)]
	[IsQuoted]
	public string[] CompilerFlags = Array.Empty<string>();

	[HelpDetails("Additional flags to pass to the linker", null)]
	[IsQuoted]
	public string[] LinkerFlags = Array.Empty<string>();

	[HelpDetails("Additional file that contains flags to pass to the linker", null)]
	[IsPath(PathKind.InputFile)]
	public string LinkerFlagsFile;

	[HelpDetails("Instruct to build an application package that can be used for testing", null)]
	public bool BuildPackageForTesting;

	[HideFromHelp]
	[IsPath(PathKind.InputFile)]
	public string[] TestPackageDataFiles = Array.Empty<string>();

	public string OutputFileExtension { get; set; } = "";

	public static IEnumerable<string> SplitFlags(string flags)
	{
		if (flags == null)
		{
			return Array.Empty<string>();
		}
		return new Regex("[ ](?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)", RegexOptions.Multiline).Split(flags);
	}
}
