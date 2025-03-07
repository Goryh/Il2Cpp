using System;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.BuildLogic;

public readonly struct MethodInstKey : IEquatable<MethodInstKey>
{
	public readonly MethodDefinition MethodDef;

	public readonly ReadOnlyCollection<TypeReference> TypeGenericArguments;

	public readonly ReadOnlyCollection<TypeReference> MethodGenericArguments;

	public MethodInstKey(MethodDefinition methodDef, TypeReference[] typeGenericArguments, TypeReference[] methodGenericArguments)
	{
		MethodDef = methodDef;
		TypeGenericArguments = typeGenericArguments?.AsReadOnly() ?? ReadOnlyCollectionCache<TypeReference>.Empty;
		MethodGenericArguments = methodGenericArguments?.AsReadOnly() ?? ReadOnlyCollectionCache<TypeReference>.Empty;
	}

	public MethodInstKey(MethodDefinition methodDef, ReadOnlyCollection<TypeReference> typeGenericArguments, ReadOnlyCollection<TypeReference> methodGenericArguments)
	{
		MethodDef = methodDef;
		TypeGenericArguments = typeGenericArguments ?? ReadOnlyCollectionCache<TypeReference>.Empty;
		MethodGenericArguments = methodGenericArguments ?? ReadOnlyCollectionCache<TypeReference>.Empty;
	}

	public override int GetHashCode()
	{
		return HashCodeHelper.Combine(MethodDef.GetHashCode(), HashCodeHelper.Combine(TypeRefIListEqualityComparer.GetHashCodeFor(TypeGenericArguments), TypeRefIListEqualityComparer.GetHashCodeFor(MethodGenericArguments)));
	}

	public bool Equals(MethodInstKey other)
	{
		if (object.Equals(MethodDef, other.MethodDef) && TypeRefIListEqualityComparer.AreEquals(TypeGenericArguments, other.TypeGenericArguments))
		{
			return TypeRefIListEqualityComparer.AreEquals(MethodGenericArguments, other.MethodGenericArguments);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is MethodInstKey other)
		{
			return Equals(other);
		}
		return false;
	}
}
