using System;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericSharing;

public static class GenericSharingAnalysis
{
	public static bool NeedsTypeContextAsArgument(MethodReference method)
	{
		if (!method.Resolve().IsStatic)
		{
			return method.DeclaringType.IsValueType;
		}
		return true;
	}

	public static GenericInstanceType GetSharedType(ReadOnlyContext context, TypeReference type)
	{
		if (!type.CanShare(context.Global.Services.TypeFactory))
		{
			return (GenericInstanceType)type;
		}
		return type.GetSharedType(context);
	}

	public static MethodReference GetSharedMethod(ReadOnlyContext context, MethodReference method)
	{
		if (!method.CanShare(context))
		{
			return (GenericInstanceMethod)method;
		}
		return method.GetSharedMethod(context);
	}

	public static bool ShouldTryToCallStaticConstructorBeforeMethodCall(ReadOnlyContext context, MethodReference targetMethod, MethodDefinition invokingMethod)
	{
		if (targetMethod.IsGenericHiddenMethodNeverUsed)
		{
			return false;
		}
		if (!targetMethod.HasThis || targetMethod.DeclaringType.IsValueType)
		{
			return true;
		}
		if (!invokingMethod.IsConstructor)
		{
			return false;
		}
		if (invokingMethod.DeclaringType.IsReferenceToThisTypeDefinition(targetMethod.DeclaringType))
		{
			return false;
		}
		return targetMethod.DeclaringType == invokingMethod.DeclaringType.GetBaseType(context);
	}

	public static TypeReference GetFullySharedTypeForGenericParameter(GenericParameter genericParameter)
	{
		if (genericParameter.HasNotNullableValueTypeConstraint)
		{
			throw new InvalidOperationException("Attempting to share generic parameter '" + genericParameter.FullName + "' which has a value type constraint.");
		}
		return genericParameter.Module.TypeSystem.Object;
	}

	public static bool CouldBeASharedGenericInstanceType(ReadOnlyContext context, TypeReference typeReference)
	{
		if (!(typeReference is GenericInstanceType genericInstanceType))
		{
			return false;
		}
		if (!typeReference.CanShare(context))
		{
			return false;
		}
		if (typeReference.ContainsFullySharedGenericTypes)
		{
			return true;
		}
		foreach (TypeReference ga in genericInstanceType.GenericArguments)
		{
			if (ga.IsSystemObject)
			{
				return true;
			}
			if (CouldBeASharedGenericInstanceType(context, ga))
			{
				return true;
			}
		}
		return false;
	}
}
