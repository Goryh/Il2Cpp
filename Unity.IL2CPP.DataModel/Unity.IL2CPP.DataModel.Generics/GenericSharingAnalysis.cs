using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel.Generics;

internal static class GenericSharingAnalysis
{
	private enum GenericParameterRestriction
	{
		None,
		ValueType,
		ReferenceType
	}

	public static bool MethodAndTypeHaveFullySharableGenericParameters(TypeContext context, MethodDefinition method)
	{
		if (MethodHasFullySharableGenericParameters(context, method))
		{
			if (method.DeclaringType.HasGenericParameters)
			{
				return TypeHasFullySharableGenericParameters(context, method.DeclaringType);
			}
			return true;
		}
		return false;
	}

	public static bool MethodHasFullySharableGenericParameters(TypeContext context, MethodDefinition methodDefinition)
	{
		if (methodDefinition.HasGenericParameters)
		{
			return AreFullySharableGenericParameters(context, methodDefinition.GenericParameters);
		}
		return false;
	}

	public static bool TypeHasFullySharableGenericParameters(TypeContext context, TypeDefinition typeDefinition)
	{
		if (typeDefinition.HasGenericParameters)
		{
			return AreFullySharableGenericParameters(context, typeDefinition.GenericParameters);
		}
		return false;
	}

	public static bool NeedsTypeContextAsArgument(MethodReference method)
	{
		if (!method.IsStatic)
		{
			return method.DeclaringType.IsValueType;
		}
		return true;
	}

	public static bool CanShareMethod(TypeContext context, MethodReference method, ITypeFactory typeFactory)
	{
		if (context.Parameters.DisableGenericSharing)
		{
			return false;
		}
		if (!method.IsGenericInstance && !method.DeclaringType.IsGenericInstance)
		{
			return false;
		}
		if (method.IsStaticConstructor && context.Parameters.FullGenericSharingStaticConstructors)
		{
			return true;
		}
		if (method.DeclaringType is GenericInstanceType genericInstance && !CanShareType(context, genericInstance, typeFactory))
		{
			return false;
		}
		return true;
	}

	public static bool CanShareType(TypeContext context, GenericInstanceType type, ITypeFactory typeFactory)
	{
		if (context.Parameters.DisableGenericSharing)
		{
			return false;
		}
		if (type.IsComOrWindowsRuntimeInterface(typeFactory))
		{
			return false;
		}
		return true;
	}

	public static bool IsGenericSharingForValueTypesEnabled(TypeContext context)
	{
		return context.Parameters.CanShareEnumTypes;
	}

	public static GenericInstanceType GetSharedType(TypeContext context, ITypeFactory typeFactory, TypeReference type)
	{
		if (context.Parameters.DisableGenericSharing)
		{
			return (GenericInstanceType)type;
		}
		TypeDefinition typeDefinition = type.Resolve();
		if (SharedTypeShouldBeFullGenericShared(context, type))
		{
			return typeDefinition.FullySharedType;
		}
		return GetSharedTypeNotFullyShared(context, typeFactory, type);
	}

	public static GenericInstanceType GetCollapsedSignatureType(TypeContext context, ITypeFactory typeFactory, TypeReference type)
	{
		return GetSharedTypeNotFullyShared(context, typeFactory, type);
	}

	private static GenericInstanceType GetSharedTypeNotFullyShared(TypeContext context, ITypeFactory typeFactory, TypeReference type)
	{
		TypeDefinition typeDefinition = type.Resolve();
		TypeResolver typeResolver = typeFactory.ResolverFor(type);
		return typeFactory.CreateGenericInstanceType(typeDefinition, null, GetSharedGenericArguments(context, typeFactory, typeResolver, typeDefinition));
	}

	private static TypeReference[] GetSharedGenericArguments(TypeContext context, ITypeFactory typeFactory, TypeResolver typeResolver, IGenericParameterProvider genericParameterProvider)
	{
		TypeReference[] sharedParams = new TypeReference[genericParameterProvider.GenericParameters.Count];
		for (int i = 0; i < sharedParams.Length; i++)
		{
			GenericParameter gp = genericParameterProvider.GenericParameters[i];
			sharedParams[i] = GetUnderlyingSharedType(context, typeFactory, typeResolver.Resolve(gp));
		}
		return sharedParams;
	}

	private static GenericParameterRestriction GetGenericParameterRestriction(GenericParameter genericParameter)
	{
		if (genericParameter == null)
		{
			return GenericParameterRestriction.None;
		}
		if (genericParameter.HasReferenceTypeConstraint)
		{
			return GenericParameterRestriction.ReferenceType;
		}
		if (genericParameter.HasNotNullableValueTypeConstraint)
		{
			return GenericParameterRestriction.ValueType;
		}
		foreach (GenericParameterConstraint constraint in genericParameter.Constraints)
		{
			GenericParameterRestriction constraintRestriction = GetGenericParameterConstraintRestriction(constraint);
			if (constraintRestriction != 0)
			{
				return constraintRestriction;
			}
		}
		return GenericParameterRestriction.None;
	}

	private static GenericParameterRestriction GetGenericParameterConstraintRestriction(GenericParameterConstraint constraint)
	{
		if (constraint.ConstraintType.IsSystemEnum)
		{
			return GenericParameterRestriction.ValueType;
		}
		if (constraint.ConstraintType.IsSystemValueType)
		{
			return GenericParameterRestriction.None;
		}
		if (constraint.ConstraintType.IsInterface)
		{
			return GenericParameterRestriction.None;
		}
		if (constraint.ConstraintType.IsGenericParameter)
		{
			return GenericParameterRestriction.None;
		}
		return GenericParameterRestriction.ReferenceType;
	}

	private static TypeReference GetUnderlyingSharedType(TypeContext context, ITypeFactory typeFactory, TypeReference inflatedType)
	{
		if (context.Parameters.DisableGenericSharing)
		{
			return inflatedType;
		}
		if (IsGenericSharingForValueTypesEnabled(context) && inflatedType.IsEnum)
		{
			inflatedType = context.TypeProvider.GetSharedEnumType(inflatedType);
		}
		if (inflatedType.IsValueType)
		{
			if (inflatedType.IsGenericInstance)
			{
				return GetSharedTypeNotFullyShared(context, typeFactory, inflatedType);
			}
			return inflatedType;
		}
		return inflatedType.Module.TypeSystem.Object;
	}

	public static MethodReference GetSharedMethod(TypeContext context, ITypeFactory typeFactory, MethodReference method)
	{
		if (context.Parameters.DisableGenericSharing)
		{
			return method;
		}
		if (SharedMethodShouldBeFullGenericShared(context, method))
		{
			return method.Resolve().FullySharedMethod;
		}
		TypeReference declaringType = method.DeclaringType;
		MethodDefinition methodDefinition = method.Resolve();
		if (declaringType.IsGenericInstance || declaringType.HasGenericParameters)
		{
			declaringType = GetSharedType(context, typeFactory, method.DeclaringType);
		}
		TypeResolver typeResolver = typeFactory.ResolverFor(method.DeclaringType, method);
		if (methodDefinition.HasGenericParameters)
		{
			return typeFactory.CreateGenericInstanceMethod(declaringType, methodDefinition, GetSharedGenericArguments(context, typeFactory, typeResolver, methodDefinition));
		}
		if (declaringType is GenericInstanceType genericInstanceType)
		{
			return typeFactory.CreateMethodReferenceOnGenericInstance(genericInstanceType, methodDefinition);
		}
		return method;
	}

	private static bool SharedTypeShouldBeFullGenericShared(TypeContext context, TypeReference typeReference)
	{
		if (context.Parameters.FullGenericSharingOnly)
		{
			return true;
		}
		return typeReference.ContainsFullySharedGenericTypes;
	}

	private static bool SharedMethodShouldBeFullGenericShared(TypeContext context, MethodReference methodReference)
	{
		if (context.Parameters.FullGenericSharingOnly)
		{
			return true;
		}
		if (methodReference.ContainsFullySharedGenericTypes)
		{
			return true;
		}
		if (context.Parameters.FullGenericSharingStaticConstructors && methodReference.IsStaticConstructor)
		{
			return true;
		}
		if (context.Parameters.DisableFullGenericSharing)
		{
			return false;
		}
		if (!(methodReference is GenericInstanceMethod genericInstanceMethod) || genericInstanceMethod.RecursiveGenericDepth < context.Parameters.MaximumRecursiveGenericDepth)
		{
			if (methodReference.DeclaringType is GenericInstanceType genericInstanceType)
			{
				return genericInstanceType.RecursiveGenericDepth >= context.Parameters.MaximumRecursiveGenericDepth;
			}
			return false;
		}
		return true;
	}

	private static bool AreFullySharableGenericParameters(TypeContext context, IEnumerable<GenericParameter> genericParameters)
	{
		if (context.Parameters.DisableGenericSharing)
		{
			return false;
		}
		if (!context.Parameters.DisableFullGenericSharing)
		{
			return true;
		}
		return genericParameters.All((GenericParameter gp) => !gp.HasNotNullableValueTypeConstraint);
	}

	public static GenericInstanceType GetFullySharedType(TypeContext context, ITypeFactory typeFactory, TypeDefinition typeDefinition)
	{
		return GetFullySharedType(context, typeFactory, typeDefinition, !context.Parameters.DisableFullGenericSharing);
	}

	private static GenericInstanceType GetFullySharedType(TypeContext context, ITypeFactory typeFactory, TypeDefinition typeDefinition, bool fullGenericSharing)
	{
		TypeReference[] genericArguments = GetFullySharedGenericArguments(context, typeDefinition, fullGenericSharing);
		return typeFactory.CreateGenericInstanceType(typeDefinition, typeDefinition.DeclaringType, genericArguments);
	}

	public static MethodReference GetFullySharedMethod(TypeContext context, ITypeFactory typeFactory, MethodDefinition method)
	{
		return GetFullySharedMethod(context, typeFactory, method, !context.Parameters.DisableFullGenericSharing);
	}

	public static MethodReference GetFullGenericSharingMethod(TypeContext context, ITypeFactory typeFactory, MethodDefinition method)
	{
		return GetFullySharedMethod(context, typeFactory, method, fullGenericSharing: true);
	}

	private static MethodReference GetFullySharedMethod(TypeContext context, ITypeFactory typeFactory, MethodDefinition method, bool fullGenericSharing)
	{
		if (!method.HasGenericParameters && !method.DeclaringType.HasGenericParameters)
		{
			throw new ArgumentException($"Attempting to get a fully shared method for method '{method}' which does not have any generic parameters");
		}
		TypeReference declaringType = method.DeclaringType;
		if (method.DeclaringType.HasGenericParameters)
		{
			declaringType = GetFullySharedType(context, typeFactory, method.DeclaringType, fullGenericSharing || (method.IsStaticConstructor && context.Parameters.FullGenericSharingStaticConstructors));
		}
		MethodDefinition methodDefinition = method.Resolve();
		if (methodDefinition.HasGenericParameters)
		{
			TypeReference[] genericArguments = GetFullySharedGenericArguments(context, methodDefinition, fullGenericSharing);
			return typeFactory.CreateGenericInstanceMethod(declaringType, methodDefinition, genericArguments);
		}
		if (declaringType is GenericInstanceType genericInstanceType)
		{
			return typeFactory.CreateMethodReferenceOnGenericInstance(genericInstanceType, methodDefinition);
		}
		return method;
	}

	private static TypeReference[] GetFullySharedGenericArguments(TypeContext context, IGenericParameterProvider genericParameterProvider, bool fullGenericSharing)
	{
		TypeReference[] genericArguments = new TypeReference[genericParameterProvider.GenericParameters.Count];
		for (int i = 0; i < genericParameterProvider.GenericParameters.Count; i++)
		{
			TypeReference genericArg = ((!fullGenericSharing) ? context.TypeProvider.ObjectTypeReference : (GetGenericParameterRestriction(genericParameterProvider.GenericParameters[i]) switch
			{
				GenericParameterRestriction.ValueType => context.GetIl2CppCustomType(Il2CppCustomType.Il2CppFullySharedGenericStruct), 
				GenericParameterRestriction.ReferenceType => context.TypeProvider.ObjectTypeReference, 
				_ => context.GetIl2CppCustomType(Il2CppCustomType.Il2CppFullySharedGeneric), 
			}));
			genericArguments[i] = genericArg;
		}
		return genericArguments;
	}
}
