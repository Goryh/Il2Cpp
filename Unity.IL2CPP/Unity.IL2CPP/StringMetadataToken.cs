using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public class StringMetadataToken
{
	public string Literal { get; private set; }

	public AssemblyDefinition Assembly { get; private set; }

	public StringMetadataToken(string literal, AssemblyDefinition assembly)
	{
		Literal = literal;
		Assembly = assembly;
	}
}
