using System;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.MethodWriting;

public class ResolvedInstruction
{
	public readonly Instruction Instruction;

	public readonly ResolvedInstruction Next;

	private InstructionOptimizations _instructionOptimizations;

	public OpCode OpCode => Instruction.OpCode;

	public object Operand => Instruction.Operand;

	public int Offset => Instruction.Offset;

	public virtual ResolvedTypeInfo TypeInfo
	{
		get
		{
			throw new InvalidCastException($"{OpCode} nas no type information.");
		}
	}

	public virtual ResolvedParameter ParameterInfo
	{
		get
		{
			throw new InvalidCastException($"{OpCode} nas no parameter information.");
		}
	}

	public virtual ResolvedVariable VariableInfo
	{
		get
		{
			throw new InvalidCastException($"{OpCode} nas no variable information.");
		}
	}

	public virtual ResolvedFieldInfo FieldInfo
	{
		get
		{
			throw new InvalidCastException($"{OpCode} nas no field information.");
		}
	}

	public virtual ResolvedMethodInfo MethodInfo
	{
		get
		{
			throw new InvalidCastException($"{OpCode} nas no method information.");
		}
	}

	public virtual ResolvedCallSiteInfo CallSiteInfo
	{
		get
		{
			throw new InvalidCastException($"{OpCode} nas no callsite information.");
		}
	}

	public InstructionOptimizations Optimization => _instructionOptimizations;

	public ResolvedInstruction(Instruction instruction, ResolvedInstruction next)
	{
		Instruction = instruction;
		Next = next;
	}

	public override string ToString()
	{
		return Instruction.ToString();
	}

	public void SetCustomOpCode(Il2CppCustomOpCode opCode)
	{
		_instructionOptimizations.CustomOpCode = opCode;
	}
}
