using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP;

internal static class ConversionOpCodes
{
	internal static void WriteNumericConversionWithOverflow<TMaxValue>(MethodBodyWriterContext methodBodyWriterContext, ResolvedTypeInfo typeReference, bool treatInputAsUnsigned, TMaxValue maxValue)
	{
		if (!WriteNumericConversionFullGenericSharing(methodBodyWriterContext, typeReference, checkOverflow: false, treatInputAsUnsigned))
		{
//			WriteCheckForOverflow(methodBodyWriterContext, typeReference, treatInputAsUnsigned, maxValue);
			WriteNumericConversion(methodBodyWriterContext, typeReference);
		}
	}

	internal static void ConvertToNaturalIntWithOverflow<TMaxValueType>(MethodBodyWriterContext methodBodyWriterContext, ResolvedTypeInfo pointerType, bool treatInputAsUnsigned, TMaxValueType maxValue)
	{
		if (!WriteNumericConversionFullGenericSharing(methodBodyWriterContext, pointerType, checkOverflow: false, treatInputAsUnsigned))
		{
//			WriteCheckForOverflow(methodBodyWriterContext, pointerType, treatInputAsUnsigned, maxValue);
			ConvertToNaturalInt(methodBodyWriterContext, pointerType);
		}
	}

	internal static void WriteNumericConversionToFloatFromUnsigned(MethodBodyWriterContext methodBodyWriterContext)
	{
		StackInfo top = methodBodyWriterContext.ValueStack.Peek();
		ReadOnlyContext context = methodBodyWriterContext.Context;
		if (top.Type.MetadataType == MetadataType.Single || top.Type.MetadataType == MetadataType.Double)
		{
			WriteNumericConversion(methodBodyWriterContext, top.Type, context.Global.Services.TypeProvider.Resolved.DoubleTypeReference);
		}
		else if (top.Type.MetadataType == MetadataType.Int64 || top.Type.MetadataType == MetadataType.UInt64)
		{
			WriteNumericConversion(methodBodyWriterContext, context.Global.Services.TypeProvider.Resolved.UInt64TypeReference, context.Global.Services.TypeProvider.Resolved.DoubleTypeReference);
		}
		else
		{
			WriteNumericConversion(methodBodyWriterContext, context.Global.Services.TypeProvider.Resolved.UInt32TypeReference, context.Global.Services.TypeProvider.Resolved.DoubleTypeReference);
		}
	}

	internal static void WriteNumericConversionI8(MethodBodyWriterContext methodBodyWriterContext)
	{
		ReadOnlyContext context = methodBodyWriterContext.Context;
		if (methodBodyWriterContext.ValueStack.Peek().Type.MetadataType == MetadataType.UInt32)
		{
			WriteNumericConversion(methodBodyWriterContext, context.Global.Services.TypeProvider.Resolved.Int32TypeReference);
		}
		WriteNumericConversion(methodBodyWriterContext, context.Global.Services.TypeProvider.Resolved.Int64TypeReference, context.Global.Services.TypeProvider.Resolved.Int64TypeReference);
	}

	internal static void WriteNumericConversionU8(MethodBodyWriterContext methodBodyWriterContext)
	{
		ReadOnlyContext context = methodBodyWriterContext.Context;
		ResolvedTypeInfo typeToCheck = methodBodyWriterContext.ValueStack.Peek().Type;
		if (typeToCheck.IsEnum())
		{
			typeToCheck = typeToCheck.GetUnderlyingEnumType();
		}
		if (StackTypeConverter.StackTypeFor(context, typeToCheck).IsSameType(context.Global.Services.TypeProvider.Resolved.Int32TypeReference.ResolvedType))
		{
			WriteNumericConversion(methodBodyWriterContext, context.Global.Services.TypeProvider.Resolved.UInt32TypeReference);
		}
		if (typeToCheck.IsSameType(context.Global.Services.TypeProvider.Resolved.IntPtrTypeReference))
		{
			WriteNumericConversion(methodBodyWriterContext, context.Global.Services.TypeProvider.Resolved.UIntPtrTypeReference);
		}
		WriteNumericConversion(methodBodyWriterContext, context.Global.Services.TypeProvider.Resolved.UInt64TypeReference, context.Global.Services.TypeProvider.Resolved.Int64TypeReference);
	}

	internal static void WriteNumericConversionFloat(MethodBodyWriterContext methodBodyWriterContext, ResolvedTypeInfo outputType)
	{
		ReadOnlyContext context = methodBodyWriterContext.Context;
		Stack<StackInfo> valueStack = methodBodyWriterContext.ValueStack;
		if (valueStack.Peek().Type.MetadataType == MetadataType.UInt32)
		{
			WriteNumericConversion(methodBodyWriterContext, context.Global.Services.TypeProvider.Resolved.Int32TypeReference, outputType);
		}
		else if (valueStack.Peek().Type.MetadataType == MetadataType.UInt64)
		{
			WriteNumericConversion(methodBodyWriterContext, context.Global.Services.TypeProvider.Resolved.Int64TypeReference, outputType);
		}
		WriteNumericConversion(methodBodyWriterContext, outputType);
	}

	internal static void WriteCheckForOverflow<TMaxValue>(MethodBodyWriterContext methodBodyWriterContext, ResolvedTypeInfo typeReference, bool treatInputAsUnsigned, TMaxValue maxValue)
	{
		ReadOnlyContext context = methodBodyWriterContext.Context;
		IGeneratedCodeWriter writer = methodBodyWriterContext.Writer;
		StackInfo value = methodBodyWriterContext.ValueStack.Peek();
		string methodInfo = methodBodyWriterContext.RuntimeMetadataAccess.MethodInfo(methodBodyWriterContext.MethodReference);
		if (value.Type.IsSameType(context.Global.Services.TypeProvider.Resolved.DoubleTypeReference) || value.Type.IsSameType(context.Global.Services.TypeProvider.Resolved.SingleTypeReference))
		{
			IGeneratedCodeWriter generatedCodeWriter = writer;
			generatedCodeWriter.WriteLine($"if ({value.Expression} > (double)({maxValue.ToString()})) {Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", methodInfo)};");
		}
		else if (treatInputAsUnsigned)
		{
			IGeneratedCodeWriter generatedCodeWriter = writer;
			generatedCodeWriter.WriteLine($"if ((uint64_t)({value.Expression}) > {maxValue.ToString()}) {Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", methodInfo)};");
		}
		else
		{
			IGeneratedCodeWriter generatedCodeWriter = writer;
			generatedCodeWriter.WriteLine($"if ((int64_t)({value.Expression}) > {maxValue.ToString()}) {Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", methodInfo)};");
		}
	}

	internal static void WriteNumericConversion(MethodBodyWriterContext methodBodyWriterContext, ResolvedTypeInfo inputType)
	{
		WriteNumericConversion(methodBodyWriterContext, inputType, inputType);
	}

	internal static void WriteNumericConversion(MethodBodyWriterContext methodBodyWriterContext, ResolvedTypeInfo inputType, ResolvedTypeInfo outputType)
	{
		if (WriteNumericConversionFullGenericSharing(methodBodyWriterContext, inputType))
		{
			return;
		}
		ReadOnlyContext context = methodBodyWriterContext.Context;
		Stack<StackInfo> valueStack = methodBodyWriterContext.ValueStack;
		StackInfo right = valueStack.Pop();
		if ((right.Type.IsSameType(context.Global.Services.TypeProvider.Resolved.SingleTypeReference) || right.Type.IsSameType(context.Global.Services.TypeProvider.Resolved.DoubleTypeReference)) && inputType.ResolvedType.IsUnsignedIntegralType)
		{
			valueStack.Push(new StackInfo($"il2cpp_codegen_cast_floating_point<{context.Global.Services.Naming.ForVariable(inputType)}, {context.Global.Services.Naming.ForVariable(outputType)}, {context.Global.Services.Naming.ForVariable(right.Type)}>({right})", outputType));
			return;
		}
		string cast = string.Empty;
		if ((right.Type.IsSameType(context.Global.Services.TypeProvider.Resolved.SingleTypeReference) || right.Type.IsSameType(context.Global.Services.TypeProvider.Resolved.DoubleTypeReference)) && inputType.ResolvedType.IsSignedIntegralType)
		{
			valueStack.Push(new StackInfo($"il2cpp_codegen_cast_double_to_int<{context.Global.Services.Naming.ForVariable(outputType)}>({right})", outputType));
			return;
		}
		if (right.Type.MetadataType == MetadataType.Pointer)
		{
			cast = "(intptr_t)";
		}
		if (!inputType.IsSameType(outputType))
		{
			cast = "(" + context.Global.Services.Naming.ForVariable(inputType) + ")" + cast;
		}
		cast = $"(({context.Global.Services.Naming.ForVariable(outputType)}){cast}{right.Expression})";
		valueStack.Push(new StackInfo(cast, outputType));
	}

	private static bool WriteNumericConversionFullGenericSharing(MethodBodyWriterContext methodBodyWriterContext, ResolvedTypeInfo outputType, bool checkOverflow = false, bool treatInputAsUnsigned = false)
	{
		Stack<StackInfo> valueStack = methodBodyWriterContext.ValueStack;
		ReadOnlyContext context = methodBodyWriterContext.Context;
		IRuntimeMetadataAccess runtimeMetadataAccess = methodBodyWriterContext.RuntimeMetadataAccess;
		if (!valueStack.Peek().Type.GetRuntimeStorage(context).IsVariableSized())
		{
			return false;
		}
		StackInfo value = methodBodyWriterContext.ValueStack.Pop();
		string checkOverflowArg = (checkOverflow ? "true" : "false");
		string treadInputAsUnsignedArg = (treatInputAsUnsigned ? "true" : "false");
		string methodInfo = (checkOverflow ? runtimeMetadataAccess.MethodInfo(methodBodyWriterContext.MethodReference) : "NULL");
		string inputTypeInfo = runtimeMetadataAccess.TypeInfoFor(value.Type);
		string conversionExpression = $"il2cpp_codegen_conv<{outputType.ResolvedType.CppNameForVariable},{checkOverflowArg},{treadInputAsUnsignedArg}>({inputTypeInfo}, {value.Expression}, {methodInfo})";
		valueStack.Push(new StackInfo(conversionExpression, outputType));
		return true;
	}

	internal static void ConvertToNaturalInt(MethodBodyWriterContext methodBodyWriterContext, ResolvedTypeInfo pointerType)
	{
		if (!WriteNumericConversionFullGenericSharing(methodBodyWriterContext, pointerType))
		{
			ReadOnlyContext context = methodBodyWriterContext.Context;
			Stack<StackInfo> valueStack = methodBodyWriterContext.ValueStack;
			StackInfo value = valueStack.Pop();
			valueStack.Push(new StackInfo($"(({context.Global.Services.Naming.ForVariable(pointerType)}){value.Expression})", pointerType));
		}
	}
}
