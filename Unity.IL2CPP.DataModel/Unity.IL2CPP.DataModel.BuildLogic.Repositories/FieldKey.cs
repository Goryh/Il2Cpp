using System;

namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

public readonly struct FieldKey : IEquatable<FieldKey>
{
	public readonly FieldReference Field;

	public readonly GenericInstanceType DeclaringType;

	public FieldKey(FieldReference field, GenericInstanceType declaringType)
	{
		Field = field;
		DeclaringType = declaringType;
	}

	public bool Equals(FieldKey other)
	{
		if (object.Equals(Field, other.Field))
		{
			return object.Equals(DeclaringType, other.DeclaringType);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is FieldKey other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Field.GetHashCode() * 397) ^ DeclaringType.GetHashCode();
	}

	public override string ToString()
	{
		return $"{DeclaringType}.{Field.Name}";
	}
}
