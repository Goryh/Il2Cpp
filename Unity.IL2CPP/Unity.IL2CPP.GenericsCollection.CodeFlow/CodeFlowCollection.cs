using System.Collections.Generic;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow;

internal class CodeFlowCollection : ICodeFlowCollection
{
	private readonly HashSet<TypeReference> _instantiatedGenericsAndArrays = new HashSet<TypeReference>();

	private readonly HashSet<GenericInstanceType> _genericTypes = new HashSet<GenericInstanceType>();

	public CodeFlowCollectionResults Complete()
	{
		return new CodeFlowCollectionResults(_instantiatedGenericsAndArrays.AsReadOnly(), _genericTypes.AsReadOnly());
	}

	public bool AddInstantiatedGeneric(GenericInstanceType type)
	{
		return _instantiatedGenericsAndArrays.Add(type);
	}

	public bool AddInstantiatedArray(ArrayType type)
	{
		return _instantiatedGenericsAndArrays.Add(type);
	}

	public void AddFoundGenericType(GenericInstanceType type)
	{
		_genericTypes.Add(type);
	}
}
