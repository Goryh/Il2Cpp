using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow;

internal struct MethodDependency
{
	public readonly MethodReference Method;

	public readonly int DefinitionIndex;

	public readonly bool IsOfInterest;

	public MethodDependency(MethodReference method, int definitionIndex, bool isOfInterest)
	{
		Method = method;
		DefinitionIndex = definitionIndex;
		IsOfInterest = isOfInterest;
	}
}
