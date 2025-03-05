using System;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow;

internal struct MethodReferenceDefinitionPair : IEquatable<MethodReferenceDefinitionPair>, IHasDefinition
{
	public readonly MethodDefinition Definition;

	public readonly MethodReference Reference;

	public MethodReferenceDefinitionPair(MethodDefinition definition, MethodReference reference)
	{
		Definition = definition;
		Reference = reference;
	}

	public bool Equals(MethodReferenceDefinitionPair other)
	{
		if (Definition != other.Definition)
		{
			return false;
		}
		return Reference == other.Reference;
	}

	public IMemberDefinition GetDefinition()
	{
		return Definition;
	}

	public override int GetHashCode()
	{
		return Reference.GetHashCode();
	}
}
