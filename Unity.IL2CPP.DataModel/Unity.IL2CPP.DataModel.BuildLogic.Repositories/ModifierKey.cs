using System;

namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

public readonly struct ModifierKey : IEquatable<ModifierKey>
{
	public readonly TypeReference ModifierType;

	public readonly TypeReference ElementType;

	public ModifierKey(TypeReference modifierType, TypeReference elementType)
	{
		ModifierType = modifierType;
		ElementType = elementType;
	}

	public bool Equals(ModifierKey other)
	{
		if (object.Equals(ModifierType, other.ModifierType))
		{
			return object.Equals(ElementType, other.ElementType);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ModifierKey other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (ModifierType.GetHashCode() * 397) ^ ElementType.GetHashCode();
	}

	public override string ToString()
	{
		return $"[modreq/modopt]({ModifierType}) {ElementType}";
	}
}
