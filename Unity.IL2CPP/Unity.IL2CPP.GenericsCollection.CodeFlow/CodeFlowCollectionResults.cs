using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow;

internal class CodeFlowCollectionResults
{
	public readonly ReadOnlyHashSet<TypeReference> InstantiatedGenericsAndArrays;

	public readonly ReadOnlyHashSet<GenericInstanceType> GenericTypes;

	public CodeFlowCollectionResults(ReadOnlyHashSet<TypeReference> instantiatedGenericsAndArrays, ReadOnlyHashSet<GenericInstanceType> genericTypes)
	{
		InstantiatedGenericsAndArrays = instantiatedGenericsAndArrays;
		GenericTypes = genericTypes;
	}
}
