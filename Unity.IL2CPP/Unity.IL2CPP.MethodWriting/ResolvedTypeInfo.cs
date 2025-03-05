using System;
using System.Diagnostics;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.MethodWriting;

[DebuggerDisplay("{UnresolvedType.FullName} - {ResolvedType.FullName}")]
public class ResolvedTypeInfo
{
	public readonly TypeReference UnresolvedType;

	public readonly TypeReference ResolvedType;

	public string FullName => UnresolvedType.FullName;

	public string Name => UnresolvedType.Name;

	public bool IsPointer => ResolvedType.IsPointer;

	public bool IsFunctionPointer => ResolvedType.IsFunctionPointer;

	public bool IsPrimitive => ResolvedType.IsPrimitive;

	public bool IsArray => ResolvedType.IsArray;

	public bool IsTypeSpecification => ResolvedType is TypeSpecification;

	public bool IsSealed => ResolvedType.Resolve().IsSealed;

	public MetadataType MetadataType => ResolvedType.MetadataType;

	public bool IsByReference => ResolvedType.IsByReference;

	public bool IsSystemEnum => ResolvedType.IsSystemEnum;

	public bool IsGenericInstance => ResolvedType.IsGenericInstance;

	public bool IsIl2CppFullySharedGenericType => ResolvedType.IsIl2CppFullySharedGenericType;

	public static ResolvedTypeInfo FromResolvedType(TypeReference resolvedType)
	{
		if (resolvedType.ContainsGenericParameter)
		{
			throw new ArgumentException("Type must be fully resolved", "resolvedType");
		}
		return Create(resolvedType, resolvedType);
	}

	public ResolvedTypeInfo(TypeReference unresolvedType, TypeReference resolvedType)
	{
		if (resolvedType.ContainsGenericParameter)
		{
			throw new ArgumentException("Type must be fully resolved", "resolvedType");
		}
		if (unresolvedType.Name.Contains("Il2CppFullySharedGenericAny"))
		{
			throw new ArgumentException("The un-resolved type must not be the fully shared type Il2CppFullySharedGenericAny ", "unresolvedType");
		}
		UnresolvedType = unresolvedType;
		ResolvedType = resolvedType;
	}

	private static ResolvedTypeInfo Create(TypeReference unresolvedType, TypeReference resolvedType)
	{
		return new ResolvedTypeInfo(unresolvedType, resolvedType);
	}

	public RuntimeStorageKind GetRuntimeStorage(ReadOnlyContext context)
	{
		return ResolvedType.GetRuntimeStorage(context);
	}

	public RuntimeStorageKind GetRuntimeStorage(ITypeFactory typeFactory)
	{
		return ResolvedType.GetRuntimeStorage(typeFactory);
	}

	public RuntimeFieldLayoutKind GetRuntimeFieldLayout(ReadOnlyContext context)
	{
		return ResolvedType.GetRuntimeFieldLayout(context);
	}

	public RuntimeFieldLayoutKind GetRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		return ResolvedType.GetRuntimeFieldLayout(typeFactory);
	}

	public bool IsNullableGenericInstance()
	{
		return ResolvedType.IsNullableGenericInstance;
	}

	public bool IsVoid()
	{
		return ResolvedType.MetadataType == MetadataType.Void;
	}

	public bool IsNotVoid()
	{
		return ResolvedType.MetadataType != MetadataType.Void;
	}

	public bool IsIntegralType()
	{
		return ResolvedType.IsIntegralType;
	}

	public bool IsUnknownSharedType()
	{
		return UnresolvedType.IsGenericParameter;
	}

	public bool IsEnum()
	{
		return ResolvedType.IsEnum;
	}

	public ResolvedTypeInfo GetUnderlyingEnumType()
	{
		return Create(UnresolvedType, ResolvedType.GetUnderlyingEnumType());
	}

	public ResolvedTypeInfo GetNonPinnedAndNonByReferenceType()
	{
		return Create(UnresolvedType.GetNonPinnedAndNonByReferenceType(), ResolvedType.GetNonPinnedAndNonByReferenceType());
	}

	public bool IsInterface()
	{
		return ResolvedType.IsInterface;
	}

	public bool IsIntegralPointerType()
	{
		return ResolvedType.IsIntegralPointerType;
	}

	public bool IsSystemObject()
	{
		return ResolvedType.IsSystemObject;
	}

	public bool IsDelegate()
	{
		return ResolvedType.IsDelegate;
	}

	public bool IsReturnedByRef(ReadOnlyContext context)
	{
		return GetRuntimeStorage(context).IsVariableSized();
	}

	public bool IsSameType(TypeReference typeReference)
	{
		return ResolvedType == typeReference;
	}

	public bool IsSameTypeInCodegen(TypeReference typeReference)
	{
		return ResolvedType == typeReference;
	}

	public bool IsSameType(ResolvedTypeInfo type)
	{
		return UnresolvedType == type.UnresolvedType;
	}

	public bool IsSameTypeInCodegen(ResolvedTypeInfo type)
	{
		return ResolvedType == type.ResolvedType;
	}

	public ResolvedTypeInfo GetElementType()
	{
		if (IsTypeSpecification)
		{
			return Create(((TypeSpecification)UnresolvedType).ElementType, ((TypeSpecification)ResolvedType).ElementType);
		}
		return this;
	}

	public ResolvedTypeInfo MakeArrayType(ReadOnlyContext context)
	{
		IDataModelService typeFactory = context.Global.Services.TypeFactory;
		ArrayType unresolvedType = typeFactory.CreateArrayType(UnresolvedType);
		if (unresolvedType == ResolvedType)
		{
			return Create(unresolvedType, unresolvedType);
		}
		return Create(unresolvedType, typeFactory.CreateArrayType(ResolvedType));
	}

	public ResolvedTypeInfo MakeByReferenceType(ReadOnlyContext context)
	{
		return MakeByReferenceType(context.Global.Services.TypeFactory);
	}

	public ResolvedTypeInfo MakeByReferenceType(ITypeFactory typeFactory)
	{
		ByReferenceType unresolved = typeFactory.CreateByReferenceType(UnresolvedType);
		if (unresolved == ResolvedType)
		{
			return Create(unresolved, unresolved);
		}
		return Create(unresolved, typeFactory.CreateByReferenceType(ResolvedType));
	}

	public ResolvedTypeInfo MakePointerType(ReadOnlyContext context)
	{
		IDataModelService typeFactory = context.Global.Services.TypeFactory;
		PointerType unresolved = typeFactory.CreatePointerType(UnresolvedType);
		if (unresolved == ResolvedType)
		{
			return Create(unresolved, unresolved);
		}
		return Create(unresolved, typeFactory.CreatePointerType(ResolvedType));
	}

	public ResolvedTypeInfo GetNullableUnderlyingType()
	{
		TypeReference resolvedType = ((GenericInstanceType)ResolvedType).GenericArguments[0];
		return Create((UnresolvedType as GenericInstanceType)?.GenericArguments[0] ?? resolvedType, resolvedType);
	}

	public override string ToString()
	{
		if (UnresolvedType.ContainsGenericParameter)
		{
			return UnresolvedType.FullName + " - " + ResolvedType.FullName;
		}
		return ResolvedType.FullName;
	}

	public override int GetHashCode()
	{
		return HashCodeHelper.Combine(ResolvedType.GetHashCode(), UnresolvedType.GetHashCode());
	}

	public override bool Equals(object obj)
	{
		if (obj is ResolvedTypeInfo other)
		{
			return IsSameType(other);
		}
		return false;
	}
}
