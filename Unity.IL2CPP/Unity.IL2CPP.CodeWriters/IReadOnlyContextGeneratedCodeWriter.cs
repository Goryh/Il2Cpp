using Unity.IL2CPP.CppDeclarations;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.CodeWriters;

public interface IReadOnlyContextGeneratedCodeWriter : ICppCodeWriter, ICodeWriter, IDirectWriter
{
	ICppDeclarations Declarations { get; }

	void AddInclude(TypeReference type);

	void AddForwardDeclaration(TypeReference type);

	void WriteExternForIl2CppType(IIl2CppRuntimeType type);

	void WriteExternForIl2CppGenericInst(IIl2CppRuntimeType[] type);

	void WriteExternForGenericClass(TypeReference type);

	void WriteExternForArray(ArrayType type);

	void Write(GeneratedCodeString other);

	void Write(IGeneratedCodeBuilder other);
}
