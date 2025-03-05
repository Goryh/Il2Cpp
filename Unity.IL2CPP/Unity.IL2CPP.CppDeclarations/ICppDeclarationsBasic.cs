using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.CppDeclarations;

public interface ICppDeclarationsBasic
{
	ReadOnlyHashSet<string> Includes { get; }

	ReadOnlyHashSet<string> RawTypeForwardDeclarations { get; }

	ReadOnlyHashSet<string> RawMethodForwardDeclarations { get; }
}
