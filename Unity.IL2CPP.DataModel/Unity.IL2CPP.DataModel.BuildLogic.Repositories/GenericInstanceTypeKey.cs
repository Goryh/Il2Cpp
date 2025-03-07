using System;
using System.Collections.ObjectModel;
using System.Text;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

public readonly struct GenericInstanceTypeKey : IEquatable<GenericInstanceTypeKey>
{
	public readonly TypeDefinition TypeDefinition;

	public readonly ReadOnlyCollection<TypeReference> GenericArguments;

	public GenericInstanceTypeKey(TypeDefinition typeDefinition, ReadOnlyCollection<TypeReference> genericArguments)
	{
		TypeDefinition = typeDefinition;
		GenericArguments = genericArguments;
	}

	public GenericInstanceTypeKey(TypeDefinition typeDefinition, TypeReference[] genericArguments)
	{
		TypeDefinition = typeDefinition;
		GenericArguments = genericArguments.AsReadOnly();
	}

	public bool Equals(GenericInstanceTypeKey other)
	{
		if (object.Equals(TypeDefinition, other.TypeDefinition))
		{
			return TypeRefIListEqualityComparer.AreEquals(GenericArguments, other.GenericArguments);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is GenericInstanceTypeKey other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCodeHelper.Combine(TypeDefinition.GetHashCode(), TypeRefIListEqualityComparer.GetHashCodeFor(GenericArguments));
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(TypeDefinition);
		sb.Append('<');
		for (int i = 0; i < GenericArguments.Count; i++)
		{
			if (i > 0)
			{
				sb.Append(", ");
			}
			sb.Append(GenericArguments[i]);
		}
		sb.Append('>');
		return sb.ToString();
	}
}
