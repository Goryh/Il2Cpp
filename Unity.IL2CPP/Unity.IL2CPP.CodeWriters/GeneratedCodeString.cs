using Unity.IL2CPP.CppDeclarations;

namespace Unity.IL2CPP.CodeWriters;

public class GeneratedCodeString
{
	public readonly string Value;

	public readonly ICppDeclarations Declarations;

	public GeneratedCodeString(string s, ICppDeclarations cppDeclarations)
	{
		Value = s;
		Declarations = cppDeclarations;
	}
}
