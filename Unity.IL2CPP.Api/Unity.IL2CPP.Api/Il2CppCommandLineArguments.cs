using Unity.Api.Attributes;

namespace Unity.IL2CPP.Api;

[ContainsOptions]
public sealed class Il2CppCommandLineArguments
{
	[Analytics("Whether or not il2cpp was used to generate code")]
	[HelpDetails("Convert the provided assemblies to C++", null)]
	public bool ConvertToCpp;

	[Analytics("Whether or not il2cpp was used to compile code")]
	[HelpDetails("Compile generated C++ code", null)]
	public bool CompileCpp;

	[HelpDetails("Use this to run the conversion in a bee graph for fast 0 change builds", null)]
	public bool ConvertInGraph;

	[HideFromHelp]
	public string CustomIl2CppRoot;

	public ConversionRequest ConversionRequest { get; set; } = new ConversionRequest();

	public CompilationRequest CompilationRequest { get; set; } = new CompilationRequest();

	public SettingsForConversionAndCompilation SettingsForConversionAndCompilation { get; set; } = new SettingsForConversionAndCompilation();
}
