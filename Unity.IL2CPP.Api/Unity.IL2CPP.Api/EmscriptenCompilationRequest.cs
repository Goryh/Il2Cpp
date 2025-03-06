using Unity.Api.Attributes;

namespace Unity.IL2CPP.Api;

[ContainsOptions]
public class EmscriptenCompilationRequest
{
	[HideFromHelp]
	[IsPath(PathKind.InputFile)]
	public string[] JsPre;

	[HideFromHelp]
	[IsPath(PathKind.InputFile)]
	public string[] JsLibraries;

	[HideFromHelp]
	[IsPath(PathKind.NeitherInputNorOutput)]
	public string EmscriptenTemp;

	[HideFromHelp]
	[IsPath(PathKind.NeitherInputNorOutput)]
	public string EmscriptenCache;
}
