using System;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.CodeWriters;

public static class GeneratedCodeWriterExtensions
{
	public static TypeReference GetForwardDeclarationType(TypeReference typeReference)
	{
		typeReference = typeReference.WithoutModifiers();
		if (typeReference is PointerType pointerType)
		{
			return GetForwardDeclarationType(pointerType.ElementType);
		}
		if (typeReference is ByReferenceType byReferenceType)
		{
			return GetForwardDeclarationType(byReferenceType.ElementType);
		}
		return typeReference;
	}

	public static void AddIncludesForTypeReference(this IReadOnlyContextGeneratedCodeWriter builder, ReadOnlyContext context, TypeReference typeReference, bool requiresCompleteType = false)
	{
		TypeReference type = typeReference.WithoutModifiers();
		if (type.ContainsGenericParameter)
		{
			return;
		}
		if (type is ArrayType arrayType)
		{
			builder.AddForwardDeclaration(arrayType);
		}
		if (type is GenericInstanceType genericInstanceType)
		{
			if (genericInstanceType.ElementType.IsValueType)
			{
				builder.AddIncludeForType(context, genericInstanceType);
			}
			else
			{
				builder.AddForwardDeclaration(genericInstanceType);
			}
		}
		if (type is ByReferenceType byRefType)
		{
			type = byRefType.ElementType;
		}
		if (type is PointerType pointerType)
		{
			type = pointerType.ElementType;
		}
		if (type.IsPrimitive)
		{
			if (type.MetadataType == MetadataType.IntPtr || type.MetadataType == MetadataType.UIntPtr)
			{
				builder.AddIncludeForType(context, type);
			}
			return;
		}
		bool isValueType = type.IsValueType;
		if (isValueType || (requiresCompleteType && !(type is TypeSpecification)))
		{
			builder.AddIncludeForType(context, type);
		}
		if (!isValueType)
		{
			builder.AddForwardDeclaration(type);
		}
	}

	public static void AddIncludeForTypeDefinition(this IReadOnlyContextGeneratedCodeWriter builder, ReadOnlyContext context, TypeReference typeReference)
	{
		TypeReference type = typeReference.WithoutModifiers();
		if (type.ContainsGenericParameter)
		{
			if (type.IsGenericParameter)
			{
				return;
			}
			TypeDefinition typeDef = type.Resolve();
			if (typeDef == null || typeDef.IsEnum)
			{
				return;
			}
		}
		if (type is ByReferenceType byRefType)
		{
			builder.AddIncludeForTypeDefinition(context, byRefType.ElementType);
		}
		else if (type is PointerType pointerType)
		{
			builder.AddIncludeForTypeDefinition(context, pointerType.ElementType);
		}
		else
		{
			builder.AddIncludeForType(context, type);
		}
	}

	public static void AddIncludeOrExternForTypeDefinition(this IReadOnlyContextGeneratedCodeWriter builder, ReadOnlyContext context, TypeReference type)
	{
		type = type.WithoutModifiers();
		if (type is ByReferenceType byReferenceType)
		{
			type = byReferenceType.ElementType;
		}
		while (type is PointerType pointerType)
		{
			type = pointerType.ElementType;
		}
		if (!type.IsValueType)
		{
			builder.AddForwardDeclaration(type);
		}
		builder.AddIncludeForType(context, type);
	}

	private static void AddIncludeForType(this IReadOnlyContextGeneratedCodeWriter builder, ReadOnlyContext context, TypeReference type)
	{
		type = type.WithoutModifiers();
		if (!type.HasGenericParameters && (!type.IsInterface || type.Resolve().Fields.Any() || type.IsComOrWindowsRuntimeInterface(context)))
		{
			if (type.IsArray)
			{
				ArrayType arrayType = (ArrayType)type;
				builder.AddIncludeOrExternForTypeDefinition(context, arrayType.ElementType);
				builder.WriteExternForArray(arrayType);
			}
			else
			{
				builder.AddInclude(type);
			}
		}
	}

	public static string InitializerStringFor(TypeReference type)
	{
		if (type.IsEnum)
		{
			return "0";
		}
		if (type.IsPrimitive)
		{
			return InitializerStringForPrimitiveType(type);
		}
		if (!type.IsValueType)
		{
			return "NULL";
		}
		return null;
	}

	public static string InitializerStringForPrimitiveType(TypeReference type)
	{
		return InitializerStringForPrimitiveType(type.MetadataType);
	}

	public static string InitializerStringForPrimitiveType(MetadataType type)
	{
		switch (type)
		{
		case MetadataType.Boolean:
			return "false";
		case MetadataType.Char:
		case MetadataType.SByte:
		case MetadataType.Byte:
			return "0x0";
		case MetadataType.Int16:
		case MetadataType.UInt16:
		case MetadataType.Int32:
		case MetadataType.UInt32:
		case MetadataType.Int64:
		case MetadataType.UInt64:
			return "0";
		case MetadataType.Double:
			return "0.0";
		case MetadataType.Single:
			return "0.0f";
		default:
			return null;
		}
	}

	public static string InitializerStringForPrimitiveCppType(string typeName)
	{
		switch (typeName)
		{
		case "bool":
			return InitializerStringForPrimitiveType(MetadataType.Boolean);
		case "char":
		case "wchar_t":
			return InitializerStringForPrimitiveType(MetadataType.Char);
		case "int64_t":
		case "uint8_t":
		case "int16_t":
		case "int32_t":
		case "int8_t":
		case "size_t":
		case "uint16_t":
		case "uint32_t":
		case "uint64_t":
			return InitializerStringForPrimitiveType(MetadataType.Int32);
		case "double":
			return InitializerStringForPrimitiveType(MetadataType.Double);
		case "float":
			return InitializerStringForPrimitiveType(MetadataType.Single);
		default:
			return null;
		}
	}

	public static void WriteVariable(this IGeneratedCodeWriter builder, ReadOnlyContext context, TypeReference type, string name)
	{
		if (type.ContainsGenericParameter)
		{
			throw new ArgumentException("Generic parameter encountered as variable type", "type");
		}
		string initialization = InitializerStringFor(type);
		string typeName = type.CppNameForVariable;
		if (initialization != null)
		{
			IGeneratedCodeWriter generatedCodeWriter = builder;
			generatedCodeWriter.WriteLine($"{typeName} {name} = {initialization};");
		}
		else
		{
			IGeneratedCodeWriter generatedCodeWriter = builder;
			generatedCodeWriter.WriteLine($"{typeName} {name};");
			builder.WriteStatement(builder.WriteMemset(Emit.AddressOf(name), 0, "sizeof(" + name + ")"));
		}
	}

	public static void WriteDefaultReturn(this IGeneratedCodeWriter builder, ReadOnlyContext context, TypeReference type)
	{
		if (type.IsVoid)
		{
			builder.WriteLine("return;");
			return;
		}
		builder.WriteVariable(context, type, "ret");
		builder.WriteLine("return ret;");
	}
}
