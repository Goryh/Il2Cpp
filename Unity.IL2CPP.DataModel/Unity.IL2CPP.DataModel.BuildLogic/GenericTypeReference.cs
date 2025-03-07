using Mono.Cecil;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.BuildLogic;

public readonly struct GenericTypeReference
{
	public Mono.Cecil.TypeReference TypeReference { get; }

	public Mono.Cecil.IGenericInstance GenericInstance { get; }

	public GenericTypeReference(Mono.Cecil.TypeReference typeReference, Mono.Cecil.IGenericInstance genericInstance)
	{
		TypeReference = typeReference;
		GenericInstance = genericInstance;
	}

	public override bool Equals(object obj)
	{
		if (obj is GenericTypeReference otherInstance)
		{
			if (otherInstance.TypeReference == TypeReference)
			{
				return GenericInstance == otherInstance.GenericInstance;
			}
			return false;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCodeHelper.Combine(TypeReference.GetHashCode(), GenericInstance.GetHashCode());
	}
}
