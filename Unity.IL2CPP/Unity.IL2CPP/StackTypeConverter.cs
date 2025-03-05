using System;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP;

public class StackTypeConverter
{
	public static ResolvedTypeInfo StackTypeFor(ReadOnlyContext context, ResolvedTypeInfo type)
	{
		if (type.ResolvedType is PinnedType)
		{
			type = type.GetElementType();
		}
		if (type.IsByReference)
		{
			return type;
		}
		if (type.IsSameType(context.Global.Services.TypeProvider.Resolved.SystemIntPtr) || type.IsSameType(context.Global.Services.TypeProvider.SystemUIntPtr) || type.IsPointer || type.IsFunctionPointer)
		{
			return context.Global.Services.TypeProvider.Resolved.SystemIntPtr;
		}
		if (!type.GetRuntimeStorage(context).IsByValue())
		{
			return context.Global.Services.TypeProvider.Resolved.ObjectTypeReference;
		}
		MetadataType metadataType = type.MetadataType;
		if (type.GetRuntimeStorage(context).IsByValue() && type.IsEnum())
		{
			metadataType = type.GetUnderlyingEnumType().MetadataType;
		}
		switch (metadataType)
		{
		case MetadataType.Boolean:
		case MetadataType.Char:
		case MetadataType.SByte:
		case MetadataType.Byte:
		case MetadataType.Int16:
		case MetadataType.UInt16:
		case MetadataType.Int32:
		case MetadataType.UInt32:
			return context.Global.Services.TypeProvider.Resolved.Int32TypeReference;
		case MetadataType.Int64:
		case MetadataType.UInt64:
			return context.Global.Services.TypeProvider.Resolved.Int64TypeReference;
		case MetadataType.Single:
			return context.Global.Services.TypeProvider.Resolved.SingleTypeReference;
		case MetadataType.Double:
			return context.Global.Services.TypeProvider.Resolved.DoubleTypeReference;
		case MetadataType.IntPtr:
		case MetadataType.UIntPtr:
			return context.Global.Services.TypeProvider.Resolved.SystemIntPtr;
		default:
			throw new ArgumentException("Cannot get stack type for " + type.Name);
		}
	}

	public static TypeReference StackTypeFor(ReadOnlyContext context, TypeReference type)
	{
		if (type is PinnedType pinnedType)
		{
			type = pinnedType.ElementType;
		}
		if (type is ByReferenceType byrefType)
		{
			return byrefType;
		}
		if (type is RequiredModifierType requiredModifierType)
		{
			return StackTypeFor(context, requiredModifierType.ElementType);
		}
		if (type is OptionalModifierType optionalModifierType)
		{
			return StackTypeFor(context, optionalModifierType.ElementType);
		}
		if (type.IsSignedOrUnsignedIntPtr() || type.IsPointer || type.IsFunctionPointer)
		{
			return context.Global.Services.TypeProvider.SystemIntPtr;
		}
		if (!type.IsValueType)
		{
			return context.Global.Services.TypeProvider.ObjectTypeReference;
		}
		MetadataType metadataType = type.MetadataType;
		if (type.IsValueType && type.IsEnum)
		{
			metadataType = type.GetUnderlyingEnumType().MetadataType;
		}
		switch (metadataType)
		{
		case MetadataType.Boolean:
		case MetadataType.Char:
		case MetadataType.SByte:
		case MetadataType.Byte:
		case MetadataType.Int16:
		case MetadataType.UInt16:
		case MetadataType.Int32:
		case MetadataType.UInt32:
			return context.Global.Services.TypeProvider.Int32TypeReference;
		case MetadataType.Int64:
		case MetadataType.UInt64:
			return context.Global.Services.TypeProvider.Int64TypeReference;
		case MetadataType.Single:
			return context.Global.Services.TypeProvider.SingleTypeReference;
		case MetadataType.Double:
			return context.Global.Services.TypeProvider.DoubleTypeReference;
		case MetadataType.IntPtr:
		case MetadataType.UIntPtr:
			return context.Global.Services.TypeProvider.SystemIntPtr;
		default:
			throw new ArgumentException("Cannot get stack type for " + type.Name);
		}
	}

	public static string CppStackTypeFor(ReadOnlyContext context, ResolvedTypeInfo type)
	{
		ResolvedTypeInfo stackType = StackTypeFor(context, type);
		if (stackType.IsByReference)
		{
			stackType = context.Global.Services.TypeProvider.Resolved.IntPtrTypeReference;
		}
		if (stackType.IsSameType(type))
		{
			return "";
		}
		return stackType.MetadataType switch
		{
			MetadataType.Int32 => "(int32_t)", 
			MetadataType.Int64 => "(int64_t)", 
			MetadataType.Double => "(double)", 
			MetadataType.Single => "(float)", 
			MetadataType.IntPtr => "(intptr_t)", 
			_ => throw new ArgumentException("Unexpected StackTypeFor: " + stackType), 
		};
	}

	public static ResolvedTypeInfo StackTypeForBinaryOperation(ReadOnlyContext context, ResolvedTypeInfo type)
	{
		ResolvedTypeInfo stackType = StackTypeFor(context, type);
		if (stackType.IsByReference)
		{
			return context.Global.Services.TypeProvider.Resolved.SystemIntPtr;
		}
		return stackType;
	}
}
