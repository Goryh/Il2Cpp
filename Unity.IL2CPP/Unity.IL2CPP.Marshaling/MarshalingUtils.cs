using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;

namespace Unity.IL2CPP.Marshaling;

public static class MarshalingUtils
{
	internal static bool IsBlittable(ReadOnlyContext context, TypeReference type, NativeType? nativeType, MarshalType marshalType, bool useUnicodeCharset)
	{
		return IsBlittable(context, type, nativeType, marshalType, useUnicodeCharset, new HashSet<TypeReference>(), new HashSet<TypeReference>(), new HashSet<TypeReference>());
	}

	private static bool IsBlittable(ReadOnlyContext context, TypeReference type, NativeType? nativeType, MarshalType marshalType, bool useUnicodeCharset, HashSet<TypeReference> previousTypes, HashSet<TypeReference> knownBlittableCache, HashSet<TypeReference> knownBlittableCacheWhenUseUnicodeCharset)
	{
		if (previousTypes.Contains(type))
		{
			return false;
		}
		if (type.IsFunctionPointer)
		{
			return true;
		}
		if (type.IsPointer)
		{
			return true;
		}
		if (type.ContainsGenericParameter || (type is TypeSpecification && !type.IsGenericInstance))
		{
			return false;
		}
		TypeDefinition typeDefinition = type.Resolve();
		useUnicodeCharset = useUnicodeCharset || (typeDefinition.Attributes & TypeAttributes.UnicodeClass) != 0;
		HashSet<TypeReference> cacheToUse = (useUnicodeCharset ? knownBlittableCacheWhenUseUnicodeCharset : knownBlittableCache);
		if (cacheToUse.Contains(type))
		{
			return true;
		}
		bool isBlittable = ((!typeDefinition.IsEnum) ? (!typeDefinition.IsSpecialSystemBaseType() && typeDefinition.GetTypeHierarchy().All((TypeDefinition t) => t.IsSpecialSystemBaseType() || t.IsSequentialLayout || t.IsExplicitLayout) && AreFieldsBlittable(context, type, nativeType, marshalType, useUnicodeCharset, previousTypes, knownBlittableCache, knownBlittableCacheWhenUseUnicodeCharset)) : IsPrimitiveBlittable(type.GetUnderlyingEnumType().Resolve(), nativeType, marshalType, useUnicodeCharset));
		if (isBlittable)
		{
			cacheToUse.Add(typeDefinition);
		}
		return isBlittable;
	}

	private static bool AreFieldsBlittable(ReadOnlyContext context, TypeReference typeRef, NativeType? nativeType, MarshalType marshalType, bool useUnicodeCharset, HashSet<TypeReference> previousTypes, HashSet<TypeReference> knownBlittableCache, HashSet<TypeReference> knownBlittableCacheWhenUseUnicodeCharset)
	{
		while (typeRef != null)
		{
			if (typeRef.IsPrimitive)
			{
				return IsPrimitiveBlittable(typeRef, nativeType, marshalType, useUnicodeCharset);
			}
			foreach (InflatedFieldType inflatedField in typeRef.GetInflatedFieldTypes(context))
			{
				if (!inflatedField.Field.IsStatic)
				{
					TypeReference fieldType = inflatedField.InflatedType;
					previousTypes.Add(typeRef);
					if (!IsBlittableAsFieldOrElementType(context, fieldType, GetFieldNativeType(inflatedField.Field), marshalType, useUnicodeCharset, previousTypes, knownBlittableCache, knownBlittableCacheWhenUseUnicodeCharset))
					{
						return false;
					}
					previousTypes.Remove(typeRef);
				}
			}
			typeRef = typeRef.GetBaseType(context);
		}
		return true;
	}

	private static bool IsBlittableAsFieldOrElementType(ReadOnlyContext context, TypeReference type, NativeType? nativeType, MarshalType marshalType, bool useUnicodeCharset, HashSet<TypeReference> previousTypes, HashSet<TypeReference> knownBlittableCache, HashSet<TypeReference> knownBlittableCacheWhenUseUnicodeCharset)
	{
		if (!type.IsValueType && !type.IsFunctionPointer && !type.IsPointer)
		{
			return false;
		}
		return IsBlittable(context, type, nativeType, marshalType, useUnicodeCharset, previousTypes, knownBlittableCache, knownBlittableCacheWhenUseUnicodeCharset);
	}

	private static NativeType? GetFieldNativeType(FieldDefinition field)
	{
		if (field.MarshalInfo == null)
		{
			return null;
		}
		if (field.MarshalInfo is ArrayMarshalInfo arrayMarshalInfo)
		{
			return arrayMarshalInfo.ElementType;
		}
		return field.MarshalInfo.NativeType;
	}

	private static bool IsPrimitiveBlittable(TypeReference type, NativeType? nativeType, MarshalType marshalType, bool useUnicodeCharset)
	{
		if (marshalType == MarshalType.ManagedLayout)
		{
			return true;
		}
		if (!nativeType.HasValue || nativeType == NativeType.Max)
		{
			if (marshalType == MarshalType.WindowsRuntime)
			{
				return true;
			}
			if (type.MetadataType == MetadataType.Char)
			{
				return useUnicodeCharset;
			}
			return type.MetadataType != MetadataType.Boolean;
		}
		switch (type.MetadataType)
		{
		case MetadataType.Boolean:
		case MetadataType.SByte:
		case MetadataType.Byte:
			if (nativeType != NativeType.U1)
			{
				return nativeType == NativeType.I1;
			}
			return true;
		case MetadataType.Char:
		case MetadataType.Int16:
		case MetadataType.UInt16:
			if (nativeType != NativeType.U2)
			{
				return nativeType == NativeType.I2;
			}
			return true;
		case MetadataType.Int32:
		case MetadataType.UInt32:
			if (nativeType != NativeType.U4)
			{
				return nativeType == NativeType.I4;
			}
			return true;
		case MetadataType.Int64:
		case MetadataType.UInt64:
			if (nativeType != NativeType.U8)
			{
				return nativeType == NativeType.I8;
			}
			return true;
		case MetadataType.IntPtr:
		case MetadataType.UIntPtr:
			if (nativeType != NativeType.UInt)
			{
				return nativeType == NativeType.Int;
			}
			return true;
		case MetadataType.Single:
			return nativeType == NativeType.R4;
		case MetadataType.Double:
			return nativeType == NativeType.R8;
		default:
			throw new ArgumentException(type.FullName + " is not a primitive!");
		}
	}

	internal static bool IsStringBuilder(TypeReference type)
	{
		return type.IsStringBuilder;
	}

	internal static IEnumerable<FieldDefinition> NonStaticFieldsOf(TypeDefinition typeDefinition)
	{
		return typeDefinition.Fields.Where((FieldDefinition field) => !field.IsStatic);
	}

	internal static bool UseUnicodeAsDefaultMarshalingForStringParameters(MethodReference method)
	{
		MethodDefinition methodDef = method.Resolve();
		if (methodDef.HasPInvokeInfo)
		{
			if (!methodDef.PInvokeInfo.IsCharSetUnicode)
			{
				return methodDef.PInvokeInfo.IsCharSetAuto;
			}
			return true;
		}
		return false;
	}

	internal static bool UseUnicodeAsDefaultMarshalingForFields(TypeReference type)
	{
		TypeDefinition typeDefinition = type.Resolve();
		if (!typeDefinition.IsUnicodeClass)
		{
			return typeDefinition.IsAutoClass;
		}
		return true;
	}

	public static string MarshalTypeToString(MarshalType marshalType)
	{
		return marshalType switch
		{
			MarshalType.PInvoke => "pinvoke", 
			MarshalType.COM => "com", 
			MarshalType.WindowsRuntime => "windows_runtime", 
			_ => throw new ArgumentException($"Unexpected MarshalType value '{marshalType}'.", "marshalType"), 
		};
	}

	public static string MarshalTypeToNiceString(MarshalType marshalType)
	{
		return marshalType switch
		{
			MarshalType.PInvoke => "P/Invoke", 
			MarshalType.COM => "COM", 
			MarshalType.WindowsRuntime => "Windows Runtime", 
			_ => throw new ArgumentException($"Unexpected MarshalType value '{marshalType}'.", "marshalType"), 
		};
	}

	public static IEnumerable<FieldDefinition> GetMarshaledFields(ReadOnlyContext context, TypeReference type, MarshalType marshalType)
	{
		if (type is GenericInstanceType genericInstanceType)
		{
			return (from t in genericInstanceType.GetTypeHierarchyWithInflatedGenericTypes(context)
				where t == type || MarshalDataCollector.MarshalInfoWriterFor(context, t, marshalType).HasNativeStructDefinition
				select t).SelectMany((TypeReference t) => NonStaticFieldsOf(t.Resolve()));
		}
		return (from t in type.Resolve().GetTypeHierarchy()
			where t == type || MarshalDataCollector.MarshalInfoWriterFor(context, t, marshalType).HasNativeStructDefinition
			select t).SelectMany((TypeDefinition t) => NonStaticFieldsOf(t));
	}

	public static int GetNativeSizeWithoutPointers(ReadOnlyContext context, TypeReference type, MarshalType marshalType)
	{
		return (from f in (from f in GetMarshaledFields(context, type, marshalType)
				select (FieldType: f.FieldType, MarshalInfo: f.MarshalInfo)).DistinctWithCount()
			select MarshalDataCollector.MarshalInfoWriterFor(context, f.Item.Type, marshalType, f.Item.MarshalInfo, UseUnicodeAsDefaultMarshalingForFields(type), forByReferenceType: false, forFieldMarshaling: true).GetNativeSizeWithoutPointers(context) * f.Count).Sum();
	}

	private static IEnumerable<((TypeReference Type, MarshalInfo MarshalInfo) Item, int Count)> DistinctWithCount(this IEnumerable<(TypeReference, MarshalInfo)> data)
	{
		Dictionary<(TypeReference, MarshalInfo), int> items = new Dictionary<(TypeReference, MarshalInfo), int>();
		foreach (var item in data)
		{
			if (items.TryGetValue(item, out var count))
			{
				items[item] = count + 1;
			}
			else
			{
				items[item] = 1;
			}
		}
		return items.Select((KeyValuePair<(TypeReference, MarshalInfo), int> p) => (Key: p.Key, Value: p.Value));
	}

	public static MarshalType[] GetMarshalTypesForMarshaledType(ReadOnlyContext context, TypeReference type)
	{
		TypeReference projectedToWindowsRuntime = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(context, type);
		if ((type.Resolve().IsExposedToWindowsRuntime() || projectedToWindowsRuntime != type) && (type.MetadataType == MetadataType.ValueType || projectedToWindowsRuntime.IsWindowsRuntimeDelegate(context)))
		{
			return new MarshalType[3]
			{
				MarshalType.PInvoke,
				MarshalType.COM,
				MarshalType.WindowsRuntime
			};
		}
		return new MarshalType[2]
		{
			MarshalType.PInvoke,
			MarshalType.COM
		};
	}

	public static bool IsMarshalableArrayField(FieldDefinition field)
	{
		if (!field.FieldType.IsArray)
		{
			return false;
		}
		if (field.MarshalInfo == null)
		{
			return true;
		}
		if (field.MarshalInfo.NativeType == NativeType.FixedArray || field.MarshalInfo.NativeType == NativeType.SafeArray)
		{
			return true;
		}
		TypeReference elementType = ((ArrayType)field.FieldType).ElementType;
		if (elementType.IsPrimitive || elementType.IsEnum)
		{
			return true;
		}
		return false;
	}

	public static bool HasMarshalableLayout(TypeReference type)
	{
		if (type.MetadataType == MetadataType.ValueType)
		{
			return true;
		}
		TypeDefinition typeDefinition = type.Resolve();
		if (typeDefinition != null)
		{
			if (!typeDefinition.IsSequentialLayout)
			{
				return typeDefinition.IsExplicitLayout;
			}
			return true;
		}
		return false;
	}

	public static bool IsMarshalable(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharSet, bool forFieldMarshaling, HashSet<TypeReference> previousTypes)
	{
		if (previousTypes.Contains(type))
		{
			return false;
		}
		DefaultMarshalInfoWriter marshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, type, marshalType, marshalInfo, useUnicodeCharSet, forByReferenceType: false, forFieldMarshaling, forReturnValue: false, forNativeToManagedWrapper: false, previousTypes);
		if (marshalInfoWriter.CanMarshalTypeToNative(context))
		{
			return marshalInfoWriter.CanMarshalTypeFromNative(context);
		}
		return false;
	}
}
