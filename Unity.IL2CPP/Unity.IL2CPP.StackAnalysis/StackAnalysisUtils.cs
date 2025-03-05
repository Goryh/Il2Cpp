using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP.StackAnalysis;

public static class StackAnalysisUtils
{
	public delegate ResolvedTypeInfo ResultTypeAnalysisMethod(ReadOnlyContext context, ResolvedTypeInfo leftType, ResolvedTypeInfo rightType);

	private static readonly ReadOnlyCollection<MetadataType> _orderedTypes = new List<MetadataType>
	{
		MetadataType.Void,
		MetadataType.Boolean,
		MetadataType.Char,
		MetadataType.SByte,
		MetadataType.Byte,
		MetadataType.Int16,
		MetadataType.UInt16,
		MetadataType.Int32,
		MetadataType.UInt32,
		MetadataType.Int64,
		MetadataType.UInt64,
		MetadataType.Single,
		MetadataType.Double,
		MetadataType.String
	}.AsReadOnly();

	public static ResolvedTypeInfo GetWidestValueType(ReadOnlyContext context, IEnumerable<ResolvedTypeInfo> types)
	{
		return GetWidestValueType(context.Global.Services.TypeFactory, types);
	}

	public static ResolvedTypeInfo GetWidestValueType(ITypeFactory typeFactory, IEnumerable<ResolvedTypeInfo> types)
	{
		MetadataType widestTypeMetadata = _orderedTypes[0];
		ResolvedTypeInfo widestTypeReference = null;
		foreach (ResolvedTypeInfo typeReference in types)
		{
			if (typeReference.GetRuntimeStorage(typeFactory) == RuntimeStorageKind.ValueType && !typeReference.IsEnum() && _orderedTypes.IndexOf(typeReference.MetadataType) > _orderedTypes.IndexOf(widestTypeMetadata))
			{
				widestTypeMetadata = typeReference.MetadataType;
				widestTypeReference = typeReference;
			}
		}
		return widestTypeReference;
	}

	public static ResolvedTypeInfo ResultTypeForAdd(ReadOnlyContext context, ResolvedTypeInfo leftType, ResolvedTypeInfo rightType)
	{
		return CorrectLargestTypeFor(context, leftType, rightType);
	}

	public static ResolvedTypeInfo ResultTypeForSub(ReadOnlyContext context, ResolvedTypeInfo leftType, ResolvedTypeInfo rightType)
	{
		if (leftType.MetadataType == MetadataType.Byte || leftType.MetadataType == MetadataType.UInt16)
		{
			return context.Global.Services.TypeProvider.Resolved.Int32TypeReference;
		}
		if (leftType.MetadataType == MetadataType.Char)
		{
			return context.Global.Services.TypeProvider.Resolved.Int32TypeReference;
		}
		return CorrectLargestTypeFor(context, leftType, rightType);
	}

	public static ResolvedTypeInfo ResultTypeForMul(ReadOnlyContext context, ResolvedTypeInfo leftType, ResolvedTypeInfo rightType)
	{
		return CorrectLargestTypeFor(context, leftType, rightType);
	}

	public static ResolvedTypeInfo CorrectLargestTypeFor(ReadOnlyContext context, ResolvedTypeInfo leftType, ResolvedTypeInfo rightType)
	{
		ResolvedTypeInfo leftStackType = StackTypeConverter.StackTypeFor(context, leftType);
		ResolvedTypeInfo rightStackType = StackTypeConverter.StackTypeFor(context, rightType);
		if (leftType.IsByReference)
		{
			return leftType;
		}
		if (rightType.IsByReference)
		{
			return rightType;
		}
		if (leftType.IsPointer)
		{
			return leftType;
		}
		if (rightType.IsPointer)
		{
			return rightType;
		}
		ITypeProviderService typeProvider = context.Global.Services.TypeProvider;
		if (leftStackType.MetadataType == MetadataType.Int64 || rightStackType.MetadataType == MetadataType.Int64)
		{
			return typeProvider.Resolved.Int64TypeReference;
		}
		if (leftStackType.IsSameType(typeProvider.SystemIntPtr) || rightStackType.IsSameType(typeProvider.SystemIntPtr))
		{
			return typeProvider.Resolved.SystemIntPtr;
		}
		if (leftStackType.IsSameType(typeProvider.Int32TypeReference) && rightStackType.IsSameType(typeProvider.Int32TypeReference))
		{
			return typeProvider.Resolved.Int32TypeReference;
		}
		return leftType;
	}

	public static ResolvedTypeInfo CalculateResultTypeForNegate(ReadOnlyContext context, ResolvedTypeInfo type)
	{
		if (type.ResolvedType.IsUnsignedIntegralType)
		{
			if (type.MetadataType == MetadataType.Byte || type.MetadataType == MetadataType.UInt16 || type.MetadataType == MetadataType.UInt32)
			{
				return context.Global.Services.TypeProvider.Resolved.Int32TypeReference;
			}
			return context.Global.Services.TypeProvider.Resolved.Int64TypeReference;
		}
		return type;
	}
}
