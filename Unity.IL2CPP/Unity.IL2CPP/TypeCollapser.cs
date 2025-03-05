using System;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP;

public static class TypeCollapser
{
	public static TypeReference[] CollapseSignature(ReadOnlyContext context, MethodReference method)
	{
		if (method.ContainsGenericParameter)
		{
			throw new InvalidOperationException("Cannot collapse uninflated method " + method.FullName);
		}
		TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(method.DeclaringType, method);
		TypeReference[] data = new TypeReference[method.Parameters.Count + 1];
		data[0] = CollapseType(context, typeResolver.ResolveReturnType(method).WithoutModifiers());
		for (int i = 0; i < method.Parameters.Count; i++)
		{
			data[i + 1] = CollapseType(context, typeResolver.ResolveParameterType(method, method.Parameters[i]).WithoutModifiers());
		}
		return data;
	}

	public static TypeReference CollapseType(ReadOnlyContext context, TypeReference type)
	{
		if (type.IsByReference || type.IsPointer)
		{
			return context.Global.Services.TypeProvider.SystemVoidPointer;
		}
		if (type.GetRuntimeStorage(context).IsVariableSized())
		{
			return context.Global.Services.TypeProvider.Il2CppFullySharedGenericTypeReference;
		}
		if (type.IsFunctionPointer)
		{
			return context.Global.Services.TypeProvider.SystemVoidPointer;
		}
		if (!type.IsValueType)
		{
			return type.Module.TypeSystem.Object;
		}
		if (type.IsEnum)
		{
			type = type.GetUnderlyingEnumType();
		}
		if (type.MetadataType == MetadataType.Boolean)
		{
			return type.Module.TypeSystem.Byte;
		}
		if (type.MetadataType == MetadataType.Char)
		{
			return type.Module.TypeSystem.UInt16;
		}
		return type;
	}
}
