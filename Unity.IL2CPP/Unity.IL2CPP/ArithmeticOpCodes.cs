using System;
using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.MethodWriting;
using Unity.IL2CPP.StackAnalysis;

namespace Unity.IL2CPP;

internal static class ArithmeticOpCodes
{
	internal static void Add(ReadOnlyContext context, Stack<StackInfo> valueStack)
	{
		StackInfo right = valueStack.Pop();
		StackInfo left = valueStack.Pop();
		ResolvedTypeInfo destType = StackAnalysisUtils.ResultTypeForAdd(context, left.Type, right.Type);
		valueStack.Push(WriteWarningProtectedOperation(context, "add", left, right, destType));
	}

	internal static void Add(ReadOnlyContext context, IGeneratedMethodCodeWriter writer, OverflowCheck check, Stack<StackInfo> valueStack, Func<string> getRaiseOverflowExceptionExpression)
	{
		StackInfo right = valueStack.Pop();
		StackInfo left = valueStack.Pop();
		if (check != 0)
		{
			ResolvedTypeInfo leftStackType = StackTypeConverter.StackTypeFor(context, left.Type);
			ResolvedTypeInfo rightStackType = StackTypeConverter.StackTypeFor(context, right.Type);
			if (RequiresPointerOverflowCheck(context, leftStackType, rightStackType))
			{
				WritePointerOverflowCheckUsing64Bits(writer, "+", check, left, right, getRaiseOverflowExceptionExpression);
			}
			else if (Requires64BitOverflowCheck(leftStackType.MetadataType, rightStackType.MetadataType))
			{
				if (check == OverflowCheck.Signed)
				{
					IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"if (il2cpp_codegen_check_add_overflow((int64_t){left.Expression}, (int64_t){right.Expression}))");
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"\t{getRaiseOverflowExceptionExpression()};");
				}
				else
				{
					IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"if ((uint64_t){left.Expression} > kIl2CppUInt64Max - (uint64_t){right.Expression})");
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"\t{getRaiseOverflowExceptionExpression()};");
				}
			}
			else
			{
				WriteNarrowOverflowCheckUsing64Bits(writer, "+", check, left.Expression, right.Expression, getRaiseOverflowExceptionExpression);
			}
		}
		ResolvedTypeInfo destType = StackAnalysisUtils.ResultTypeForAdd(context, left.Type, right.Type);
		valueStack.Push(WriteWarningProtectedOperation(context, "add", left, right, destType));
	}

	internal static void Sub(ReadOnlyContext context, Stack<StackInfo> valueStack)
	{
		StackInfo right = valueStack.Pop();
		StackInfo left = valueStack.Pop();
		ResolvedTypeInfo destType = StackAnalysisUtils.ResultTypeForSub(context, left.Type, right.Type);
		valueStack.Push(WriteWarningProtectedOperation(context, "subtract", left, right, destType));
	}

	internal static void Sub(ReadOnlyContext context, IGeneratedMethodCodeWriter writer, OverflowCheck check, Stack<StackInfo> valueStack, Func<string> getRaiseOverflowExceptionExpression)
	{
		StackInfo right = valueStack.Pop();
		StackInfo left = valueStack.Pop();
		if (check != 0)
		{
			ResolvedTypeInfo leftStackType = StackTypeConverter.StackTypeFor(context, left.Type);
			ResolvedTypeInfo rightStackType = StackTypeConverter.StackTypeFor(context, right.Type);
			if (RequiresPointerOverflowCheck(context, leftStackType, rightStackType))
			{
				WritePointerOverflowCheckUsing64Bits(writer, "-", check, left, right, getRaiseOverflowExceptionExpression);
			}
			else if (Requires64BitOverflowCheck(leftStackType.MetadataType, rightStackType.MetadataType))
			{
				if (check == OverflowCheck.Signed)
				{
					IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"if (il2cpp_codegen_check_sub_overflow((int64_t){left.Expression}, (int64_t){right.Expression}))");
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"\t{getRaiseOverflowExceptionExpression()};");
				}
				else
				{
					IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"if ((uint64_t){left.Expression} < (uint64_t){right.Expression})");
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"\t{getRaiseOverflowExceptionExpression()};");
				}
			}
			else
			{
				WriteNarrowOverflowCheckUsing64Bits(writer, "-", check, left.Expression, right.Expression, getRaiseOverflowExceptionExpression);
			}
		}
		ResolvedTypeInfo destType = StackAnalysisUtils.ResultTypeForSub(context, left.Type, right.Type);
		valueStack.Push(WriteWarningProtectedOperation(context, "subtract", left, right, destType));
	}

	internal static void Mul(ReadOnlyContext context, Stack<StackInfo> valueStack)
	{
		StackInfo right = valueStack.Pop();
		StackInfo left = valueStack.Pop();
		ResolvedTypeInfo destType = StackAnalysisUtils.ResultTypeForMul(context, left.Type, right.Type);
		valueStack.Push(WriteWarningProtectedOperation(context, "multiply", left, right, destType));
	}

	internal static void Mul(ReadOnlyContext context, IGeneratedMethodCodeWriter writer, OverflowCheck check, Stack<StackInfo> valueStack, Func<string> getRaiseOverflowExceptionExpression)
	{
		StackInfo right = valueStack.Pop();
		StackInfo left = valueStack.Pop();
		if (check != 0)
		{
			ResolvedTypeInfo leftStackType = StackTypeConverter.StackTypeFor(context, left.Type);
			ResolvedTypeInfo rightStackType = StackTypeConverter.StackTypeFor(context, right.Type);
			if (RequiresPointerOverflowCheck(context, leftStackType, rightStackType))
			{
				WritePointerOverflowCheckUsing64Bits(writer, "*", check, left, right, getRaiseOverflowExceptionExpression);
			}
			else if (Requires64BitOverflowCheck(leftStackType.MetadataType, rightStackType.MetadataType))
			{
				if (check == OverflowCheck.Signed)
				{
					IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"if (il2cpp_codegen_check_mul_overflow_i64((int64_t){left.Expression}, (int64_t){right.Expression}, kIl2CppInt64Min, kIl2CppInt64Max))");
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"\t{getRaiseOverflowExceptionExpression()};");
				}
				else
				{
					IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"if (il2cpp_codegen_check_mul_oveflow_u64({left.Expression}, {right.Expression}))");
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"\t{getRaiseOverflowExceptionExpression()};");
				}
			}
			else
			{
				WriteNarrowOverflowCheckUsing64Bits(writer, "*", check, left.Expression, right.Expression, getRaiseOverflowExceptionExpression);
			}
		}
		ResolvedTypeInfo destType = StackAnalysisUtils.ResultTypeForMul(context, left.Type, right.Type);
		valueStack.Push(WriteWarningProtectedOperation(context, "multiply", left, right, destType));
	}

	private static bool RequiresPointerOverflowCheck(ReadOnlyContext context, ResolvedTypeInfo leftStackType, ResolvedTypeInfo rightStackType)
	{
		if (!RequiresPointerOverflowCheck(context, leftStackType))
		{
			return RequiresPointerOverflowCheck(context, rightStackType);
		}
		return true;
	}

	private static bool RequiresPointerOverflowCheck(ReadOnlyContext context, ResolvedTypeInfo type)
	{
		if (!type.IsSameType(context.Global.Services.TypeProvider.Resolved.SystemIntPtr))
		{
			return type.IsSameType(context.Global.Services.TypeProvider.Resolved.SystemUIntPtr);
		}
		return true;
	}

	private static bool Requires64BitOverflowCheck(MetadataType leftStackType, MetadataType rightStackType)
	{
		if (!Requires64BitOverflowCheck(leftStackType))
		{
			return Requires64BitOverflowCheck(rightStackType);
		}
		return true;
	}

	private static bool Requires64BitOverflowCheck(MetadataType metadataType)
	{
		if (metadataType != MetadataType.UInt64)
		{
			return metadataType == MetadataType.Int64;
		}
		return true;
	}

	private static void WriteNarrowOverflowCheckUsing64Bits(IGeneratedMethodCodeWriter writer, string op, OverflowCheck check, string leftExpression, string rightExpression, Func<string> getRaiseOverflowExceptionExpression)
	{
		if (check == OverflowCheck.Signed)
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if (((int64_t){leftExpression} {op} (int64_t){rightExpression} < (int64_t)kIl2CppInt32Min) || ((int64_t){leftExpression} {op} (int64_t){rightExpression} > (int64_t)kIl2CppInt32Max))");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"\t{getRaiseOverflowExceptionExpression()};");
		}
		else
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if ((uint64_t)(uint32_t){leftExpression} {op} (uint64_t)(uint32_t){rightExpression} > (uint64_t)(uint32_t)kIl2CppUInt32Max)");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"\t{getRaiseOverflowExceptionExpression()};");
		}
	}

	private static StackInfo WriteWarningProtectedOperation(ReadOnlyContext context, string name, StackInfo left, StackInfo right, ResolvedTypeInfo resultType)
	{
		string rcast = CastExpressionForBinaryOperator(context, right);
		string lcast = CastExpressionForBinaryOperator(context, left);
		if (!resultType.IsPointer)
		{
			try
			{
				resultType = StackTypeConverter.StackTypeFor(context, resultType);
			}
			catch (ArgumentException)
			{
			}
		}
		return WriteWarningProtectedOperation(context, resultType, lcast, left.Expression, rcast, right.Expression, name);
	}

	private static string CastExpressionForBinaryOperator(ReadOnlyContext context, StackInfo right)
	{
		try
		{
			return StackTypeConverter.CppStackTypeFor(context, right.Type);
		}
		catch (ArgumentException)
		{
			return "";
		}
	}

	private static StackInfo WriteWarningProtectedOperation(ReadOnlyContext context, ResolvedTypeInfo destType, string lcast, string left, string rcast, string right, string name)
	{
		return new StackInfo($"(({context.Global.Services.Naming.ForVariable(destType)})il2cpp_codegen_{name}({lcast}{left}, {rcast}{right}))", destType);
	}

	private static void WritePointerOverflowCheckUsing64Bits(IGeneratedMethodCodeWriter writer, string op, OverflowCheck check, StackInfo left, StackInfo right, Func<string> getRaiseOverflowExceptionExpression)
	{
		WritePointerOverflowCheckUsing64Bits(writer, op, check, left.Expression, right.Expression, getRaiseOverflowExceptionExpression);
	}

	private static void WritePointerOverflowCheckUsing64Bits(IGeneratedMethodCodeWriter writer, string op, OverflowCheck check, string leftExpression, string rightExpression, Func<string> getRaiseOverflowExceptionExpression)
	{
		if (check == OverflowCheck.Signed)
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if (((intptr_t){leftExpression} {op} (intptr_t){rightExpression} < (intptr_t)kIl2CppIntPtrMin) || ((intptr_t){leftExpression} {op} (intptr_t){rightExpression} > (intptr_t)kIl2CppIntPtrMax))");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"\t{getRaiseOverflowExceptionExpression()};");
		}
		else
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if ((uintptr_t){leftExpression} {op} (uintptr_t){rightExpression} > (uintptr_t)kIl2CppUIntPtrMax)");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"\t{getRaiseOverflowExceptionExpression()};");
		}
	}
}
