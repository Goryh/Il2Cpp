using System;

namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

public readonly struct ArrayKey : IEquatable<ArrayKey>
{
	public readonly TypeReference ElementType;

	public readonly int Rank;

	public ArrayKey(TypeReference elementType, int rank)
	{
		ElementType = elementType;
		Rank = rank;
	}

	public bool Equals(ArrayKey other)
	{
		if (object.Equals(ElementType, other.ElementType))
		{
			return Rank == other.Rank;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ArrayKey other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (ElementType.GetHashCode() * 397) ^ Rank;
	}

	public override string ToString()
	{
		return $"{ElementType}[{Rank}]";
	}
}
