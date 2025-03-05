using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP.StackAnalysis;

public static class StackInstructionOptimizations
{
	public static void OptimizeMethodCall(ReadOnlyContext context, ResolvedInstruction ins, ResolvedInstruction previous, List<Entry> arguments)
	{
		ResolvedTypeInfo declaringType = GetMethodDeclaringTypeConstrained(ins, previous);
		if (declaringType.IsSystemEnum && ins.MethodInfo.Parameters.Count == 1 && ins.MethodInfo.HasThis && ins.MethodInfo.Name == "HasFlag")
		{
			ResolvedInstruction instruction = arguments[0].Instruction;
			if (instruction != null && instruction.OpCode.Code == Code.Box)
			{
				ResolvedInstruction instruction2 = arguments[1].Instruction;
				if (instruction2 != null && instruction2.OpCode.Code == Code.Box && arguments[0].Instruction.TypeInfo.IsEnum() && arguments[0].Instruction.TypeInfo.IsSameType(arguments[1].Instruction.TypeInfo))
				{
					if (IsBoxInvisibleToOtherInstructions(arguments[0].Instruction))
					{
						arguments[0].Instruction.SetCustomOpCode(Il2CppCustomOpCode.CopyStackValue);
					}
					if (IsBoxInvisibleToOtherInstructions(arguments[1].Instruction))
					{
						arguments[1].Instruction.SetCustomOpCode(Il2CppCustomOpCode.CopyStackValue);
					}
					ins.SetCustomOpCode(Il2CppCustomOpCode.EnumHasFlag);
					return;
				}
			}
		}
		if (declaringType.IsEnum() && ins.MethodInfo.Parameters.Count == 0 && ins.MethodInfo.HasThis && ins.MethodInfo.Name == "GetHashCode")
		{
			if (IsBoxInvisibleToOtherInstructions(arguments[0].Instruction))
			{
				arguments[0].Instruction.SetCustomOpCode(Il2CppCustomOpCode.CopyStackValue);
			}
			ins.SetCustomOpCode(Il2CppCustomOpCode.EnumGetHashCode);
		}
		else if (ins.MethodInfo.IsGenericInstance && ins.MethodInfo.UnresovledMethodReference.Resolve()?.FullName == "System.Boolean System.Runtime.CompilerServices.RuntimeHelpers::IsReferenceOrContainsReferences()")
		{
			TypeReference genericArgument = ((GenericInstanceMethod)ins.MethodInfo.ResolvedMethodReference).GenericArguments[0];
			switch (genericArgument.GetRuntimeStorage(context))
			{
			case RuntimeStorageKind.ReferenceType:
				ins.SetCustomOpCode(Il2CppCustomOpCode.PushTrue);
				break;
			case RuntimeStorageKind.Pointer:
				ins.SetCustomOpCode(Il2CppCustomOpCode.PushFalse);
				break;
			case RuntimeStorageKind.ValueType:
				if (genericArgument.IsReferenceOrContainsReferenceTypeFields(context))
				{
					ins.SetCustomOpCode(Il2CppCustomOpCode.PushTrue);
				}
				else
				{
					ins.SetCustomOpCode(Il2CppCustomOpCode.PushFalse);
				}
				break;
			case RuntimeStorageKind.VariableSizedValueType:
			case RuntimeStorageKind.VariableSizedAny:
				break;
			}
		}
		else if (ins.MethodInfo.IsGenericInstance && ins.MethodInfo.UnresovledMethodReference.Resolve()?.FullName == "System.Boolean Unity.Collections.LowLevel.Unsafe.UnsafeUtility::IsUnmanaged()")
		{
			TypeReference genericArgument2 = ((GenericInstanceMethod)ins.MethodInfo.ResolvedMethodReference).GenericArguments[0];
			switch (genericArgument2.GetRuntimeStorage(context))
			{
			case RuntimeStorageKind.Pointer:
			case RuntimeStorageKind.ReferenceType:
				ins.SetCustomOpCode(Il2CppCustomOpCode.PushFalse);
				break;
			case RuntimeStorageKind.ValueType:
				if (genericArgument2.IsReferenceOrContainsReferenceTypeFields(context))
				{
					ins.SetCustomOpCode(Il2CppCustomOpCode.PushFalse);
				}
				else
				{
					ins.SetCustomOpCode(Il2CppCustomOpCode.PushTrue);
				}
				break;
			case RuntimeStorageKind.VariableSizedValueType:
			case RuntimeStorageKind.VariableSizedAny:
				break;
			}
		}
		else if (ins.MethodInfo.DeclaringType.IsSystemObject() && ins.MethodInfo.IsStatic() && ins.MethodInfo.Parameters.Count == 2 && ins.MethodInfo.Name == "ReferenceEquals")
		{
			OptimizeComparison(context, ins, arguments[0], arguments[1]);
		}
	}

	private static ResolvedTypeInfo GetMethodDeclaringTypeConstrained(ResolvedInstruction ins, ResolvedInstruction previous)
	{
		if (ins.OpCode.Code == Code.Callvirt && previous != null && previous.OpCode.Code == Code.Constrained)
		{
			return previous.TypeInfo;
		}
		return ins.MethodInfo.DeclaringType;
	}

	public static void OptimizeBox(ReadOnlyContext context, ResolvedInstruction ins, ResolvedInstructionBlock block)
	{
		if (ins.TypeInfo.GetRuntimeStorage(context) == RuntimeStorageKind.ReferenceType)
		{
			ins.SetCustomOpCode(Il2CppCustomOpCode.Nop);
		}
	}

	public static void OptimizeComparison(ReadOnlyContext context, ResolvedInstruction ins, Entry left, Entry right)
	{
		ResolvedInstruction instruction = right.Instruction;
		int num;
		if (instruction != null && instruction.OpCode.Code == Code.Box)
		{
			ResolvedInstruction instruction2 = left.Instruction;
			num = ((instruction2 != null && instruction2.OpCode.Code == Code.Ldnull) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool isRightBoxCompareToNull = (byte)num != 0;
		ResolvedInstruction instruction3 = left.Instruction;
		int num2;
		if (instruction3 != null && instruction3.OpCode.Code == Code.Box)
		{
			ResolvedInstruction instruction4 = right.Instruction;
			num2 = ((instruction4 != null && instruction4.OpCode.Code == Code.Ldnull) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		bool isLeftBoxCompareToNull = (byte)num2 != 0;
		if (!(isLeftBoxCompareToNull || isRightBoxCompareToNull))
		{
			return;
		}
		ResolvedInstruction boxInstruction = (isRightBoxCompareToNull ? right.Instruction : left.Instruction);
		ResolvedInstruction ldNullInstruction = (isRightBoxCompareToNull ? left.Instruction : right.Instruction);
		ResolvedTypeInfo boxedType = boxInstruction.TypeInfo;
		if (!IsBoxInvisibleToOtherInstructions(boxInstruction))
		{
			return;
		}
		bool valueTypeResultIsTrue;
		switch (ins.OpCode.Code)
		{
		case Code.Call:
		case Code.Ceq:
			valueTypeResultIsTrue = false;
			break;
		case Code.Cgt:
		case Code.Cgt_Un:
			valueTypeResultIsTrue = isRightBoxCompareToNull;
			break;
		case Code.Clt:
		case Code.Clt_Un:
			valueTypeResultIsTrue = isLeftBoxCompareToNull;
			break;
		default:
			throw new InvalidOperationException($"Unhandled comparision case {ins.Instruction.OpCode.Code}");
		}
		switch (boxedType.GetRuntimeStorage(context))
		{
		case RuntimeStorageKind.Pointer:
		case RuntimeStorageKind.ReferenceType:
			break;
		case RuntimeStorageKind.ValueType:
		case RuntimeStorageKind.VariableSizedValueType:
			ldNullInstruction.SetCustomOpCode(Il2CppCustomOpCode.Nop);
			if (boxedType.IsNullableGenericInstance())
			{
				boxInstruction.SetCustomOpCode(valueTypeResultIsTrue ? Il2CppCustomOpCode.NullableIsNotNull : Il2CppCustomOpCode.NullableIsNull);
				ins.SetCustomOpCode(Il2CppCustomOpCode.CopyStackValue);
			}
			else
			{
				boxInstruction.SetCustomOpCode(Il2CppCustomOpCode.Pop1);
				ins.SetCustomOpCode(valueTypeResultIsTrue ? Il2CppCustomOpCode.PushTrue : Il2CppCustomOpCode.PushFalse);
			}
			break;
		case RuntimeStorageKind.VariableSizedAny:
			ldNullInstruction.SetCustomOpCode(Il2CppCustomOpCode.Nop);
			boxInstruction.SetCustomOpCode(valueTypeResultIsTrue ? Il2CppCustomOpCode.VariableSizedWouldBoxToNotNull : Il2CppCustomOpCode.VariableSizedWouldBoxToNull);
			ins.SetCustomOpCode(Il2CppCustomOpCode.CopyStackValue);
			break;
		}
	}

	public static void OptimizeBrTrue(ReadOnlyContext context, ResolvedInstruction ins, ResolvedInstructionBlock block, Entry entry, ReadOnlyCollection<ResolvedInstructionBlock> blocks)
	{
		ResolvedInstruction prevIns = entry.Instruction;
		if (prevIns != null && prevIns.Optimization.CustomOpCode == Il2CppCustomOpCode.PushFalse)
		{
			ApplyAlwaysTakenBranchOptimization(Il2CppCustomOpCode.BranchLeft, ins, block, blocks);
		}
		else if (prevIns != null && prevIns.Optimization.CustomOpCode == Il2CppCustomOpCode.PushTrue)
		{
			ApplyAlwaysTakenBranchOptimization(Il2CppCustomOpCode.BranchRight, ins, block, blocks);
		}
		else if (prevIns != null && prevIns.OpCode.Code == Code.Box)
		{
			ApplyBoxBranchOptimization(context, Il2CppCustomOpCode.BranchRight, ins, prevIns, block, blocks);
		}
	}

	public static void OptimizeBrFalse(ReadOnlyContext context, ResolvedInstruction ins, ResolvedInstructionBlock block, Entry entry, ReadOnlyCollection<ResolvedInstructionBlock> blocks)
	{
		ResolvedInstruction prevIns = entry.Instruction;
		if (prevIns != null && prevIns.Optimization.CustomOpCode == Il2CppCustomOpCode.PushFalse)
		{
			ApplyAlwaysTakenBranchOptimization(Il2CppCustomOpCode.BranchRight, ins, block, blocks);
		}
		else if (prevIns != null && prevIns.Optimization.CustomOpCode == Il2CppCustomOpCode.PushTrue)
		{
			ApplyAlwaysTakenBranchOptimization(Il2CppCustomOpCode.BranchLeft, ins, block, blocks);
		}
		else if (prevIns != null && prevIns.OpCode.Code == Code.Box)
		{
			ApplyBoxBranchOptimization(context, Il2CppCustomOpCode.BranchLeft, ins, prevIns, block, blocks);
		}
	}

	private static void ApplyBoxBranchOptimization(ReadOnlyContext context, Il2CppCustomOpCode boxBranchOptimizationTaken, ResolvedInstruction branchInstruction, ResolvedInstruction boxInstruction, ResolvedInstructionBlock block, ReadOnlyCollection<ResolvedInstructionBlock> blocks)
	{
		if (IsBoxInvisibleToOtherInstructions(boxInstruction))
		{
			ResolvedTypeInfo typeReference = boxInstruction.TypeInfo;
			if (typeReference.IsNullableGenericInstance())
			{
				boxInstruction.SetCustomOpCode(Il2CppCustomOpCode.NullableBoxBranchOptimization);
			}
			else if (typeReference.GetRuntimeStorage(context).IsByValue())
			{
				boxInstruction.SetCustomOpCode(Il2CppCustomOpCode.BoxBranchOptimization);
				ApplyAlwaysTakenBranchOptimization(boxBranchOptimizationTaken, branchInstruction, block, blocks);
			}
			else if (typeReference.GetRuntimeStorage(context).IsVariableSized())
			{
				boxInstruction.SetCustomOpCode(Il2CppCustomOpCode.VariableSizedBoxBranchOptimization);
			}
		}
	}

	private static void ApplyAlwaysTakenBranchOptimization(Il2CppCustomOpCode takenBranchCode, ResolvedInstruction ins, ResolvedInstructionBlock block, ReadOnlyCollection<ResolvedInstructionBlock> blocks)
	{
		ins.SetCustomOpCode(takenBranchCode);
		Instruction notTakenBranchFirstIns;
		switch (takenBranchCode)
		{
		case Il2CppCustomOpCode.BranchLeft:
			notTakenBranchFirstIns = (Instruction)ins.Operand;
			break;
		case Il2CppCustomOpCode.BranchRight:
			notTakenBranchFirstIns = block.Block.Last.Next;
			break;
		default:
			throw new InvalidOperationException();
		}
		ResolvedInstructionBlock notTakenBlock = blocks.Single((ResolvedInstructionBlock b) => b.Block.First == notTakenBranchFirstIns);
		block.Block.MarkSuccessorNotTaken(notTakenBlock.Block);
	}

	public static void OptimizeLdsfld(ReadOnlyContext context, ResolvedInstruction ins)
	{
		if (ins.OpCode.Code == Code.Ldsfld)
		{
			if (ins.FieldInfo.DeclaringType.IsIntegralPointerType() && ins.FieldInfo.Name == "Zero")
			{
				ins.SetCustomOpCode(Il2CppCustomOpCode.LdsfldZero);
			}
			else if (ins.FieldInfo.DeclaringType.IsSameType(context.Global.Services.TypeProvider.GetSystemType(SystemType.BitConverter)) && ins.FieldInfo.Name == "IsLittleEndian")
			{
				ins.SetCustomOpCode(Il2CppCustomOpCode.BitConverterIsLittleEndian);
			}
		}
	}

	private static bool IsBoxInvisibleToOtherInstructions(ResolvedInstruction boxInstruction)
	{
		if (boxInstruction != null && boxInstruction.Instruction.OpCode.Code == Code.Box)
		{
			ResolvedInstruction next = boxInstruction.Next;
			if (next == null)
			{
				return true;
			}
			return next.OpCode.Code != Code.Dup;
		}
		return false;
	}
}
