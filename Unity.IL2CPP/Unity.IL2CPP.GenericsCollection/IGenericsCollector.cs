using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericsCollection;

public interface IGenericsCollector
{
	bool AddArray(ArrayType type);

	bool AddTypeDeclaration(GenericInstanceType type);

	bool AddType(GenericInstanceType type);

	bool AddMethod(GenericInstanceMethod method);
}
