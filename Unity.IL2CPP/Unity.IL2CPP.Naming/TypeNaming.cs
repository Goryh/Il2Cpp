using System;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Naming;

public static class TypeNaming
{
	public static string ForType(this INamingService naming, TypeReference typeReference)
	{
		typeReference = typeReference.WithoutModifiers();
		return typeReference.CppName;
	}

	public static string ForRuntimeType(this INamingService naming, ReadOnlyContext context, TypeReference typeReference)
	{
		typeReference = typeReference.WithoutModifiers();
		return naming.ForRuntimeUniqueTypeNameOnly(context, typeReference);
	}

	public static string ForIl2CppType(this INamingService naming, ReadOnlyContext context, IIl2CppRuntimeType runtimeType)
	{
		TypeReference nonPinnedAndNonByReferenceType = runtimeType.Type.GetNonPinnedAndNonByReferenceType();
		string typeName = (nonPinnedAndNonByReferenceType.IsGenericParameter ? naming.ForGenericParameter((GenericParameter)nonPinnedAndNonByReferenceType) : ((runtimeType.Attrs == 0 && !(runtimeType.Type is TypeSpecification)) ? runtimeType.Type.WithoutModifiers().CppName : naming.ForRuntimeUniqueTypeNameOnly(context, runtimeType.Type.WithoutModifiers())));
		return typeName + "_" + (runtimeType.Type.IsByReference ? 1 : 0) + "_" + (runtimeType.Type.IsPinned ? 1 : 0) + "_" + runtimeType.Attrs;
	}

	public static string ForGenericParameter(this INamingService naming, GenericParameter genericParameter)
	{
		string typeName = genericParameter.Type switch
		{
			GenericParameterType.Type => ((TypeReference)genericParameter.Owner).CppName, 
			GenericParameterType.Method => ((MethodReference)genericParameter.Owner).CppName, 
			_ => throw new InvalidOperationException($"Unhandled {"GenericParameterType"} case {genericParameter.Owner.GenericParameterType}"), 
		};
		return $"{typeName}_gp_{genericParameter.Position}";
	}
}
