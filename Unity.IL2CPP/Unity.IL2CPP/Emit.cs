using System;
using System.Collections.Generic;
using System.Text;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.MethodWriting;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP;

public class Emit
{
	public const string ClassMetadataType = "RuntimeClass";

	public const string MethodMetadataType = "RuntimeMethod";

	public const string FieldMetadataType = "RuntimeField";

	public const string TypeMetadataType = "RuntimeType";

	public static string Arrow(string left, string right)
	{
		return left + "->" + right;
	}

	public static string Assign(string left, string right)
	{
		return left + " = " + right;
	}

	public static string Box(ReadOnlyContext context, TypeReference type, string value, IRuntimeMetadataAccess metadataAccess)
	{
		if (!type.IsValueType)
		{
			return Cast(context, type, value);
		}
		return Call(context, "Box", metadataAccess.TypeInfoFor(type), "&" + value);
	}

	public static string Call(ReadOnlyContext context, string method)
	{
		return Call(context, method, Array.Empty<string>());
	}

	public static string Call(ReadOnlyContext context, string method, string argument)
	{
		return Call(context, method, new string[1] { argument });
	}

	public static string Call(ReadOnlyContext context, string method, string argument1, string argument2)
	{
		return Call(context, method, new string[2] { argument1, argument2 });
	}

	public static string Call(ReadOnlyContext context, string method, string argument1, string argument2, string argument3)
	{
		return Call(context, method, new string[3] { argument1, argument2, argument3 });
	}

	public static string Call(ReadOnlyContext context, string method, string[] arguments)
	{
		using Returnable<StringBuilder> builderContext = context.Global.Services.Factory.CheckoutStringBuilder();
		StringBuilder value = builderContext.Value;
		value.Append(method);
		value.Append('(');
		value.AppendAggregateWithComma(arguments);
		value.Append(')');
		return value.ToString();
	}

	public static string Call(ReadOnlyContext context, string method, IReadOnlyList<string> arguments)
	{
		using Returnable<StringBuilder> builderContext = context.Global.Services.Factory.CheckoutStringBuilder();
		StringBuilder value = builderContext.Value;
		value.Append(method);
		value.Append('(');
		value.AppendAggregateWithComma(arguments);
		value.Append(')');
		return value.ToString();
	}

	public static string Cast(ReadOnlyContext context, TypeReference type, string value)
	{
		return "(" + type.CppNameForVariable + ")" + value;
	}

	public static string Cast(ReadOnlyContext context, ResolvedTypeInfo type, string value)
	{
		return "(" + context.Global.Services.Naming.ForVariable(type) + ")" + value;
	}

	public static string CastToPointer(ReadOnlyContext context, TypeReference type, string value)
	{
		return "(" + type.CppNameForPointerToVariable + ")" + value;
	}

	public static string CastToPointer(ReadOnlyContext context, ResolvedTypeInfo type, string value)
	{
		return "(" + context.Global.Services.Naming.ForPointerToVariable(type) + ")" + value;
	}

	public static string CastToByReferenceType(ReadOnlyContext context, TypeReference type, string value)
	{
		return "(" + type.CppNameForReferenceToVariable + ")" + value;
	}

	public static string CastToByReferenceType(ReadOnlyContext context, ResolvedTypeInfo type, string value)
	{
		return "(" + context.Global.Services.Naming.ForReferenceToVariable(type) + ")" + value;
	}

	public static IEnumerable<string> CastEach(string targetTypeName, IEnumerable<string> values)
	{
		List<string> casts = new List<string>();
		foreach (string value in values)
		{
			casts.Add("(" + targetTypeName + ")" + value);
		}
		return casts;
	}

	public static string Cast(string type, string value)
	{
		return "(" + type + ")" + value;
	}

	public static string InitializedTypeInfo(string argument)
	{
		return "InitializedTypeInfo(" + argument + ")";
	}

	public static string AddressOf(string value)
	{
		if (value.StartsWith("*"))
		{
			return value.Substring(1);
		}
		return "(&" + value + ")";
	}

	public static string Dereference(string value)
	{
		if (value.StartsWith("&"))
		{
			return value.Substring(1);
		}
		if (value.StartsWith("(&") && value.EndsWith(")"))
		{
			return value.Substring(2, value.Length - 3);
		}
		return "*" + value;
	}

	public static string Dot(string left, string right)
	{
		return left + "." + right;
	}

	public static string InParentheses(string expression)
	{
		return "(" + expression + ")";
	}

	public static string ArrayBoundsCheck(string array, string index)
	{
		return MultiDimensionalArrayBoundsCheck("(uint32_t)(" + array + ")->max_length", index);
	}

	public static string MultiDimensionalArrayBoundsCheck(string length, string index)
	{
		return $"IL2CPP_ARRAY_BOUNDS_CHECK({index}, {length});";
	}

	public static string MultiDimensionalArrayBoundsCheck(ReadOnlyContext context, string array, string index, int rank)
	{
		return ArrayBoundsCheck(array, index);
	}

	public static string LoadArrayElement(string array, string index, bool useArrayBoundsCheck)
	{
		return $"({array})->{ArrayNaming.ForArrayItemGetter(useArrayBoundsCheck)}(static_cast<{ArrayNaming.ForArrayIndexType()}>({index}))";
	}

	public static string LoadArrayElementAddress(string array, string index, bool useArrayBoundsCheck)
	{
		return $"({array})->{ArrayNaming.ForArrayItemAddressGetter(useArrayBoundsCheck)}(static_cast<{ArrayNaming.ForArrayIndexType()}>({index}))";
	}

	public static string StoreArrayElement(string array, string index, string value, bool useArrayBoundsCheck)
	{
		return $"({array})->{ArrayNaming.ForArrayItemSetter(useArrayBoundsCheck)}(static_cast<{ArrayNaming.ForArrayIndexType()}>({index}), {value})";
	}

	public static string NewObj(ReadOnlyContext context, TypeReference type, IRuntimeMetadataAccess metadataAccess)
	{
		string callExpression = Call(context, "il2cpp_codegen_object_new", metadataAccess.TypeInfoFor(type));
		if (type.IsValueType)
		{
			return callExpression;
		}
		return Cast(type.CppName + "*", callExpression);
	}

	public static string NewSZArray(ReadOnlyContext context, ArrayType arrayType, int length, IRuntimeMetadataAccess metadataAccess)
	{
		return NewSZArray(context, arrayType, arrayType, length.ToString(), metadataAccess);
	}

	public static string NewSZArray(ReadOnlyContext context, ArrayType arrayType, string length, IRuntimeMetadataAccess metadataAccess)
	{
		return NewSZArray(context, arrayType, arrayType, length, metadataAccess);
	}

	public static string NewSZArray(ReadOnlyContext context, ArrayType arrayType, ArrayType unresolvedArrayType, string length, IRuntimeMetadataAccess metadataAccess)
	{
		if (arrayType.Rank != 1)
		{
			throw new ArgumentException("Attempting for create a new sz array of invalid rank.", "arrayType");
		}
		return Cast(context, arrayType, Call(context, "SZArrayNew", metadataAccess.ArrayInfo(unresolvedArrayType), length));
	}

	public static string Memset(ReadOnlyContext context, string address, int value, string size)
	{
		return Call(context, "memset", address, value.ToString(), size);
	}

	public static string ArrayElementTypeCheck(string array, string value)
	{
		return $"ArrayElementTypeCheck ({array}, {value});";
	}

	public static string DivideByZeroCheck(TypeReference type, string denominator)
	{
		if (!type.IsIntegralType)
		{
			return string.Empty;
		}
		return "DivideByZeroCheck(" + denominator + ")";
	}

	public static string RaiseManagedException(string exception, string throwingMethodInfo = null)
	{
		if (exception == "NULL")
		{
			return "IL2CPP_RAISE_NULL_REFERENCE_EXCEPTION()";
		}
		if (string.IsNullOrEmpty(throwingMethodInfo))
		{
			throwingMethodInfo = "NULL";
		}
		return $"IL2CPP_RAISE_MANAGED_EXCEPTION({exception}, {throwingMethodInfo})";
	}

	public static string RethrowManagedException(string exception)
	{
		return "IL2CPP_RETHROW_MANAGED_EXCEPTION(" + exception + ")";
	}

	public static string NullCheck(string name)
	{
		return "NullCheck(" + name + ")";
	}

	public static string MemoryBarrier()
	{
		return "il2cpp_codegen_memory_barrier()";
	}

	public static string VariableSizedAnyForArgLoad(IRuntimeMetadataAccess runtimeMetadataAccess, TypeReference type, string expression)
	{
		return VariableSizedAnyForArgLoad(runtimeMetadataAccess.TypeInfoFor(type, IRuntimeMetadataAccess.TypeInfoForReason.IsValueType), expression);
	}

	public static string VariableSizedAnyForArgLoad(string typeInfo, string expression)
	{
		return $"(il2cpp_codegen_class_is_value_type({typeInfo}) ? {expression} : &{expression})";
	}

	public static string VariableSizedAnyForArgPassing(IRuntimeMetadataAccess runtimeMetadataAccess, TypeReference type, string valueTypeExpression, string referenceTypeExpression)
	{
		return VariableSizedAnyForArgPassing(runtimeMetadataAccess.TypeInfoFor(type, IRuntimeMetadataAccess.TypeInfoForReason.IsValueType), valueTypeExpression, referenceTypeExpression);
	}

	public static string VariableSizedAnyForArgPassing(string typeInfo, string expression)
	{
		return VariableSizedAnyForArgPassing(typeInfo, expression, expression);
	}

	public static string VariableSizedAnyForArgPassing(string typeInfo, string valueTypeExpression, string referenceTypeExpression)
	{
		return $"(il2cpp_codegen_class_is_value_type({typeInfo}) ? {valueTypeExpression}: *(void**){referenceTypeExpression})";
	}

	public static string Comment(string input)
	{
		input = input.TrimEnd();
		byte[] bytes = Encoding.ASCII.GetBytes(input);
		int length = bytes.Length;
		for (int i = 0; i < length; i++)
		{
			if (IsUnprintableAsciiValue(bytes[i]))
			{
				bytes[i] = 63;
			}
		}
		string output = Encoding.ASCII.GetString(bytes);
		return "// " + output.TrimEnd(' ', '\t', '/', '\\');
	}

	private static bool IsUnprintableAsciiValue(byte asciiValue)
	{
		if (asciiValue >= 32)
		{
			return asciiValue >= 127;
		}
		return true;
	}
}
