using System;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.GenericsCollection;

public static class Inflater
{
	public static TypeReference InflateType(ReadOnlyContext context, GenericContext genericContext, TypeReference typeReference)
	{
		return InflateTypeWithoutException(context, genericContext, typeReference) ?? throw new InvalidOperationException($"Unable to resolve a reference to the type '{typeReference.FullName}' in the assembly '{typeReference.Module.Assembly.FullName}'. Does this type exist in a different assembly in the project?");
	}

	public static GenericInstanceType InflateType(ReadOnlyContext context, GenericContext genericContext, TypeDefinition typeDefinition)
	{
		return ConstructGenericType(context, genericContext, typeDefinition, typeDefinition.GenericParameters);
	}

	public static GenericInstanceType InflateType(ReadOnlyContext context, GenericContext genericContext, GenericInstanceType genericInstanceType)
	{
		return ConstructGenericType(context, genericContext, genericInstanceType.Resolve(), genericInstanceType.GenericArguments);
	}

	public static TypeReference InflateTypeWithoutException(ReadOnlyContext context, GenericContext genericContext, TypeReference typeReference)
	{
		if (typeReference is GenericParameter genericParameter)
		{
			TypeReference inflatedType = ((genericParameter.Type == GenericParameterType.Type) ? genericContext.Type.GenericArguments[genericParameter.Position] : genericContext.Method.GenericArguments[genericParameter.Position]);
			if (inflatedType.ContainsFullySharedGenericTypes)
			{
				inflatedType = context.Global.Services.TypeProvider.Il2CppFullySharedGenericTypeReference;
			}
			return inflatedType;
		}
		IDataModelService typeFactory = context.Global.Services.TypeFactory;
		if (typeReference is GenericInstanceType genericInstanceType)
		{
			return InflateType(context, genericContext, genericInstanceType);
		}
		if (typeReference is ArrayType arrayType)
		{
			return typeFactory.CreateArrayType(InflateType(context, genericContext, arrayType.ElementType), arrayType.Rank);
		}
		if (typeReference is ByReferenceType byReferenceType)
		{
			return typeFactory.CreateByReferenceType(InflateType(context, genericContext, byReferenceType.ElementType));
		}
		if (typeReference is PointerType pointerType)
		{
			return typeFactory.CreatePointerType(InflateType(context, genericContext, pointerType.ElementType));
		}
		if (typeReference is RequiredModifierType reqModType)
		{
			return InflateTypeWithoutException(context, genericContext, reqModType.ElementType);
		}
		if (typeReference is OptionalModifierType optModType)
		{
			return InflateTypeWithoutException(context, genericContext, optModType.ElementType);
		}
		return typeReference.Resolve();
	}

	private static GenericInstanceType ConstructGenericType(ReadOnlyContext context, GenericContext genericContext, TypeDefinition typeDefinition, ReadOnlyCollection<TypeReference> genericArguments)
	{
		TypeReference[] inflatedGenericArguments = new TypeReference[genericArguments.Count];
		for (int i = 0; i < genericArguments.Count; i++)
		{
			inflatedGenericArguments[i] = InflateType(context, genericContext, genericArguments[i]);
		}
		return context.Global.Services.TypeFactory.CreateGenericInstanceType(typeDefinition, typeDefinition.DeclaringType, inflatedGenericArguments);
	}

	private static GenericInstanceType ConstructGenericType(ReadOnlyContext context, GenericContext genericContext, TypeDefinition typeDefinition, ReadOnlyCollection<GenericParameter> genericArguments)
	{
		TypeReference[] inflatedGenericArguments = new TypeReference[genericArguments.Count];
		for (int i = 0; i < genericArguments.Count; i++)
		{
			inflatedGenericArguments[i] = InflateType(context, genericContext, genericArguments[i]);
		}
		return context.Global.Services.TypeFactory.CreateGenericInstanceType(typeDefinition, typeDefinition.DeclaringType, inflatedGenericArguments);
	}

	public static GenericInstanceMethod InflateMethod(ReadOnlyContext context, GenericContext genericContext, MethodDefinition methodDefinition)
	{
		TypeReference declaringType = methodDefinition.DeclaringType;
		if (declaringType.Resolve().HasGenericParameters)
		{
			declaringType = InflateType(context, genericContext, methodDefinition.DeclaringType);
		}
		return ConstructGenericMethod(context, genericContext, declaringType, methodDefinition, methodDefinition.GenericParameters);
	}

	public static GenericInstanceMethod InflateMethod(ReadOnlyContext context, GenericContext genericContext, GenericInstanceMethod genericInstanceMethod)
	{
		TypeReference inflatedType = ((genericInstanceMethod.DeclaringType is GenericInstanceType genericInstanceType) ? InflateType(context, genericContext, genericInstanceType) : InflateType(context, genericContext, genericInstanceMethod.DeclaringType));
		return ConstructGenericMethod(context, genericContext, inflatedType, genericInstanceMethod.Resolve(), genericInstanceMethod.GenericArguments);
	}

	private static GenericInstanceMethod ConstructGenericMethod(ReadOnlyContext context, GenericContext genericContext, TypeReference declaringType, MethodDefinition method, ReadOnlyCollection<TypeReference> genericArguments)
	{
		TypeReference[] inflatedGenericArguments = new TypeReference[genericArguments.Count];
		for (int i = 0; i < genericArguments.Count; i++)
		{
			inflatedGenericArguments[i] = InflateType(context, genericContext, genericArguments[i]);
		}
		return context.Global.Services.TypeFactory.CreateGenericInstanceMethod(declaringType, method, inflatedGenericArguments);
	}

	private static GenericInstanceMethod ConstructGenericMethod(ReadOnlyContext context, GenericContext genericContext, TypeReference declaringType, MethodDefinition method, ReadOnlyCollection<GenericParameter> genericArguments)
	{
		TypeReference[] inflatedGenericArguments = new TypeReference[genericArguments.Count];
		for (int i = 0; i < genericArguments.Count; i++)
		{
			inflatedGenericArguments[i] = InflateType(context, genericContext, genericArguments[i]);
		}
		return context.Global.Services.TypeFactory.CreateGenericInstanceMethod(declaringType, method, inflatedGenericArguments);
	}
}
