using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;

namespace Unity.IL2CPP.Marshaling;

public class MarshalDataCollector
{
	public static DefaultMarshalInfoWriter MarshalInfoWriterFor(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo = null, bool useUnicodeCharSet = false, bool forByReferenceType = false, bool forFieldMarshaling = false, bool forReturnValue = false, bool forNativeToManagedWrapper = false, HashSet<TypeReference> typesForRecursiveFields = null)
	{
		type = type.WithoutModifiers();
		if (type is TypeSpecification && !(type is ArrayType) && !(type is ByReferenceType) && !(type is PointerType) && !(type is GenericInstanceType))
		{
			return new UnmarshalableMarshalInfoWriter(context, type);
		}
		if (type is GenericParameter || type.ContainsGenericParameter || type.HasGenericParameters)
		{
			return new UnmarshalableMarshalInfoWriter(context, type);
		}
		return CreateMarshalInfoWriter(context, type, marshalType, marshalInfo, useUnicodeCharSet, forByReferenceType, forFieldMarshaling, forReturnValue, forNativeToManagedWrapper, typesForRecursiveFields);
	}

	private static DefaultMarshalInfoWriter CreateMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharSet, bool forByReferenceType, bool forFieldMarshaling, bool forReturnValue, bool forNativeToManagedWrapper, HashSet<TypeReference> typesForRecursiveFields)
	{
		return context.Global.Services.Factory.CreateMarshalInfoWriter(context, type, marshalType, marshalInfo, useUnicodeCharSet, forByReferenceType, forFieldMarshaling, forReturnValue, forNativeToManagedWrapper, typesForRecursiveFields);
	}

	public static bool HasCustomMarshalingMethods(ReadOnlyContext context, TypeReference type, NativeType? nativeType, MarshalType marshalType, bool useUnicodeCharSet, bool forFieldMarshaling)
	{
		TypeDefinition typeDef = type.Resolve();
		if (typeDef.MetadataType != MetadataType.ValueType && typeDef.MetadataType != MetadataType.Class)
		{
			return false;
		}
		if (typeDef.HasGenericParameters && (forFieldMarshaling || typeDef.Fields.Any((FieldDefinition field) => field.FieldType.ContainsGenericParameter || field.FieldType.IsGenericInstance)))
		{
			return false;
		}
		if (typeDef.IsInterface)
		{
			return false;
		}
		if (typeDef.MetadataType == MetadataType.ValueType && MarshalingUtils.IsBlittable(context, typeDef, nativeType, marshalType, useUnicodeCharSet))
		{
			return false;
		}
		if (marshalType == MarshalType.WindowsRuntime && typeDef.MetadataType != MetadataType.ValueType)
		{
			return false;
		}
		return typeDef.GetTypeHierarchy().All((TypeDefinition t) => t.IsSpecialSystemBaseType() || t.IsSequentialLayout || t.IsExplicitLayout);
	}

	public static bool FieldIsArrayOfType(FieldDefinition field, TypeReference typeRef)
	{
		if (field.FieldType is ArrayType fieldTypeArray)
		{
			return fieldTypeArray.ElementType == typeRef;
		}
		return false;
	}
}
