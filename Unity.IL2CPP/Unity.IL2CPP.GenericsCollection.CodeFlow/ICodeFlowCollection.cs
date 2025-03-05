using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow;

public interface ICodeFlowCollection
{
	bool AddInstantiatedGeneric(GenericInstanceType type);

	bool AddInstantiatedArray(ArrayType type);

	void AddFoundGenericType(GenericInstanceType type);
}
