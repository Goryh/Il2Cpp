using System;

namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

public readonly struct SystemImplementedArrayMethodKey : IEquatable<SystemImplementedArrayMethodKey>
{
	public readonly ArrayType ArrayType;

	public readonly string Name;

	public SystemImplementedArrayMethodKey(ArrayType arrayType, string name)
	{
		ArrayType = arrayType;
		Name = name;
	}

	public bool Equals(SystemImplementedArrayMethodKey other)
	{
		if (object.Equals(ArrayType, other.ArrayType))
		{
			return Name == other.Name;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is SystemImplementedArrayMethodKey other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (ArrayType.GetHashCode() * 397) ^ Name.GetHashCode();
	}
}
