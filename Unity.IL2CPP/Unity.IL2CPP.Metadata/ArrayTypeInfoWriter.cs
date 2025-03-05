using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Metadata;

public class ArrayTypeInfoWriter
{
	internal static IEnumerable<GenericInstanceMethod> InflateArrayMethods(ReadOnlyContext context, ArrayType arrayType)
	{
		return context.Global.Services.TypeProvider.GraftedArrayInterfaceMethods.Select((MethodDefinition m) => InflateArrayMethod(context, m, arrayType.ElementType));
	}

	internal static GenericInstanceMethod InflateArrayMethod(ReadOnlyContext context, MethodDefinition method, TypeReference elementType)
	{
		if (!method.HasGenericParameters)
		{
			throw new ArgumentException("Methods without generic parameters cannot be inflated");
		}
		return context.Global.Services.TypeFactory.CreateGenericInstanceMethod(method.DeclaringType, method, elementType);
	}

	internal static IEnumerable<TypeReference> TypeAndAllBaseAndInterfaceTypesFor(ReadOnlyContext context, TypeReference type)
	{
		List<TypeReference> typeAndAllBaseAndInterfaceTypes = new List<TypeReference>();
		while (type != null)
		{
			typeAndAllBaseAndInterfaceTypes.Add(type);
			foreach (TypeReference interfaceType in type.GetInterfaces(context))
			{
				if (!IsGenericInstanceWithMoreThanOneGenericArgument(interfaceType) && !interfaceType.IsGraftedArrayInterfaceType)
				{
					typeAndAllBaseAndInterfaceTypes.Add(interfaceType);
				}
			}
			type = type.GetBaseType(context);
		}
		return typeAndAllBaseAndInterfaceTypes;
	}

	private static bool IsGenericInstanceWithMoreThanOneGenericArgument(TypeReference type)
	{
		if (type.IsGenericInstance && type is GenericInstanceType { HasGenericArguments: not false } genericInstanceType && genericInstanceType.GenericArguments.Count > 1)
		{
			return true;
		}
		return false;
	}
}
