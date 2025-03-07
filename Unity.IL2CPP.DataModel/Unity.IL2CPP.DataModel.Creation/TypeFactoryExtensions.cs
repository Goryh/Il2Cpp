using System.Collections.Generic;
using System.Linq;

namespace Unity.IL2CPP.DataModel.Creation;

public static class TypeFactoryExtensions
{
	public static GenericInstanceType CreateGenericInstanceTypeFromDefinition(this ITypeFactory factory, TypeDefinition typeDefinition, params TypeReference[] genericArguments)
	{
		return factory.CreateGenericInstanceType(typeDefinition, typeDefinition.DeclaringType, genericArguments);
	}

	public static GenericInstanceType CreateGenericInstanceTypeFromDefinition(this ITypeFactory factory, TypeDefinition typeDefinition, IEnumerable<GenericParameter> genericArguments)
	{
		return factory.CreateGenericInstanceType(typeDefinition, typeDefinition.DeclaringType, genericArguments.Cast<TypeReference>().ToArray());
	}

	public static GenericInstanceMethod CreateGenericInstanceMethodFromDefinition(this ITypeFactory factory, MethodDefinition methodDefinition, params TypeReference[] genericArguments)
	{
		return factory.CreateGenericInstanceMethod(methodDefinition.DeclaringType, methodDefinition, genericArguments);
	}

	public static ITypeFactory CreateCached(this ITypeFactory typeFactory, TypeContext typeContext)
	{
		return new CachedTypeFactory(typeContext, typeFactory);
	}

	public static ArrayType CreateArrayType(this ITypeFactory typeFactory, TypeReference elementType)
	{
		return typeFactory.CreateArrayType(elementType, 1, isVector: true);
	}

	public static ArrayType CreateArrayType(this ITypeFactory typeFactory, TypeReference elementType, int rank)
	{
		return typeFactory.CreateArrayType(elementType, rank, rank == 1);
	}
}
