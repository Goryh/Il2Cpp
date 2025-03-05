using System;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP;

public class ArrayUtilities
{
	public static ResolvedTypeInfo ArrayElementTypeOf(ResolvedTypeInfo typeReference)
	{
		if (typeReference.IsArray)
		{
			return typeReference.GetElementType();
		}
		if (typeReference.IsTypeSpecification)
		{
			return ArrayElementTypeOf(typeReference.GetElementType());
		}
		throw new ArgumentException(typeReference.FullName + " is not an array type", "typeReference");
	}
}
