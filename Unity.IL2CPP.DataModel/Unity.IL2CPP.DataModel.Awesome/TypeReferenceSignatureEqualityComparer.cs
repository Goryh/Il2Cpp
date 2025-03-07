using System;

namespace Unity.IL2CPP.DataModel.Awesome;

public static class TypeReferenceSignatureEqualityComparer
{
	public static bool AreEqual(TypeReference a, TypeReference b, TypeComparisonMode comparisonMode)
	{
		if (a == b)
		{
			return true;
		}
		if (a == null || b == null)
		{
			return false;
		}
		if (a is TypeSpecification)
		{
			if (!(b is TypeSpecification))
			{
				return false;
			}
			if (a is GenericInstanceType aGenericInstanceType)
			{
				if (b is GenericInstanceType bGenericInstanceType)
				{
					return AreEqual(aGenericInstanceType, bGenericInstanceType, comparisonMode);
				}
				return false;
			}
			if (a is ArrayType aArrayType)
			{
				if (b is ArrayType bArrayType)
				{
					if (aArrayType.IsVector != bArrayType.IsVector)
					{
						return false;
					}
					if (aArrayType.Rank != bArrayType.Rank)
					{
						return false;
					}
					return AreEqual(aArrayType.ElementType, bArrayType.ElementType, comparisonMode);
				}
				return false;
			}
			if (a is ByReferenceType aByReferenceType)
			{
				if (b is ByReferenceType bByReferenceType)
				{
					return AreEqual(aByReferenceType.ElementType, bByReferenceType.ElementType, comparisonMode);
				}
				return false;
			}
			if (a is PointerType aPointerType)
			{
				if (b is PointerType bPointerType)
				{
					return AreEqual(aPointerType.ElementType, bPointerType.ElementType, comparisonMode);
				}
				return false;
			}
			if (a is RequiredModifierType aRequiredModifierType)
			{
				if (b is RequiredModifierType bRequiredModifierType)
				{
					if (AreEqual(aRequiredModifierType.ModifierType, bRequiredModifierType.ModifierType, comparisonMode))
					{
						return AreEqual(aRequiredModifierType.ElementType, bRequiredModifierType.ElementType, comparisonMode);
					}
					return false;
				}
				return false;
			}
			if (a is OptionalModifierType aOptionalModifierType)
			{
				if (b is OptionalModifierType bOptionalModifierType)
				{
					if (AreEqual(aOptionalModifierType.ModifierType, bOptionalModifierType.ModifierType, comparisonMode))
					{
						return AreEqual(aOptionalModifierType.ElementType, bOptionalModifierType.ElementType, comparisonMode);
					}
					return false;
				}
				return false;
			}
			if (a is PinnedType aPinnedType)
			{
				if (b is PinnedType bPinnedType)
				{
					return AreEqual(aPinnedType.ElementType, bPinnedType.ElementType, comparisonMode);
				}
				return false;
			}
			if (a is SentinelType aSentinelType)
			{
				if (b is SentinelType bSentinelType)
				{
					return AreEqual(aSentinelType.ElementType, bSentinelType.ElementType, comparisonMode);
				}
				return false;
			}
			throw new InvalidOperationException("Unexpected type derived from TypeSpecification encountered.");
		}
		if (b is TypeSpecification)
		{
			return false;
		}
		if (a is GenericParameter aGenericParameter)
		{
			if (b is GenericParameter bGenericParameter)
			{
				return AreEqual(aGenericParameter, bGenericParameter, comparisonMode);
			}
			return false;
		}
		if (b is GenericParameter)
		{
			return false;
		}
		if (!a.Name.Equals(b.Name) || !a.Namespace.Equals(b.Namespace))
		{
			return false;
		}
		TypeDefinition xDefinition = a.Resolve();
		TypeDefinition yDefinition = b.Resolve();
		if (xDefinition != null && yDefinition == null)
		{
			return false;
		}
		if (xDefinition == null && yDefinition != null)
		{
			return false;
		}
		if (xDefinition != null && yDefinition != null)
		{
			if (comparisonMode == TypeComparisonMode.SignatureOnlyLoose)
			{
				if (xDefinition.Module.Name != yDefinition.Module.Name)
				{
					return false;
				}
				if (xDefinition.Module.Assembly.Name.Name != yDefinition.Module.Assembly.Name.Name)
				{
					return false;
				}
			}
			if (comparisonMode == TypeComparisonMode.SignatureOnlyLoose || comparisonMode == TypeComparisonMode.SignatureOnlyLoose2)
			{
				return xDefinition.FullName == yDefinition.FullName;
			}
			return xDefinition == yDefinition;
		}
		if (a.Module.Name != b.Module.Name)
		{
			return false;
		}
		if (a.Module.Assembly.Name.Name != b.Module.Assembly.Name.Name)
		{
			return false;
		}
		return a.FullName == b.FullName;
	}

	private static bool AreEqual(GenericParameter a, GenericParameter b, TypeComparisonMode comparisonMode)
	{
		if (a == b)
		{
			return true;
		}
		if (a.Position != b.Position)
		{
			return false;
		}
		if (a.Type != b.Type)
		{
			return false;
		}
		if (a.Owner is TypeReference aOwnerType && AreEqual(aOwnerType, b.Owner as TypeReference, comparisonMode))
		{
			return true;
		}
		MethodReference aOwnerMethod = a.Owner as MethodReference;
		if (comparisonMode != TypeComparisonMode.SignatureOnlyLoose && comparisonMode != TypeComparisonMode.SignatureOnlyLoose2 && aOwnerMethod == b.Owner)
		{
			return true;
		}
		if (comparisonMode != 0 && comparisonMode != TypeComparisonMode.SignatureOnlyLoose)
		{
			return comparisonMode == TypeComparisonMode.SignatureOnlyLoose2;
		}
		return true;
	}

	private static bool AreEqual(GenericInstanceType a, GenericInstanceType b, TypeComparisonMode comparisonMode)
	{
		if (a == b)
		{
			return true;
		}
		int aGenericArgumentsCount = a.GenericArguments.Count;
		if (aGenericArgumentsCount != b.GenericArguments.Count)
		{
			return false;
		}
		if (!AreEqual(a.ElementType, b.ElementType, comparisonMode))
		{
			return false;
		}
		for (int i = 0; i < aGenericArgumentsCount; i++)
		{
			if (!AreEqual(a.GenericArguments[i], b.GenericArguments[i], comparisonMode))
			{
				return false;
			}
		}
		return true;
	}
}
