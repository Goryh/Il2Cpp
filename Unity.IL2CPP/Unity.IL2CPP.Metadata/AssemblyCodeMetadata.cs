using System.Collections.ObjectModel;

namespace Unity.IL2CPP.Metadata;

public class AssemblyCodeMetadata
{
	public readonly string CodegenModule;

	public readonly ReadOnlyCollection<RgctxEntryName> RgctxEntryNames;

	public AssemblyCodeMetadata(string codegenModule, ReadOnlyCollection<RgctxEntryName> rgctxEntryNames)
	{
		CodegenModule = codegenModule;
		RgctxEntryNames = rgctxEntryNames;
	}
}
