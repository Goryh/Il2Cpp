using System;
using Mono.Cecil.Cil;

namespace Unity.IL2CPP.DataModel;

public readonly struct OpCode : IEquatable<OpCode>
{
	public string Name { get; }

	public Code Code { get; }

	public FlowControl FlowControl { get; }

	public OpCodeType OpCodeType { get; }

	public OperandType OperandType { get; }

	public StackBehaviour StackBehaviourPop { get; }

	public StackBehaviour StackBehaviourPush { get; }

	public int Size { get; }

	internal OpCode(Mono.Cecil.Cil.OpCode opCode)
	{
		Name = opCode.Name;
		Code = (Code)opCode.Code;
		FlowControl = (FlowControl)opCode.FlowControl;
		OperandType = (OperandType)opCode.OperandType;
		OpCodeType = (OpCodeType)opCode.OpCodeType;
		StackBehaviourPop = (StackBehaviour)opCode.StackBehaviourPop;
		StackBehaviourPush = (StackBehaviour)opCode.StackBehaviourPush;
		Size = opCode.Size;
	}

	public bool Equals(OpCode other)
	{
		return other.Code == Code;
	}

	public override bool Equals(object obj)
	{
		if (obj is OpCode other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)Code;
	}

	public static bool operator ==(OpCode thisCode, OpCode other)
	{
		return thisCode.Equals(other);
	}

	public static bool operator !=(OpCode thisCode, OpCode other)
	{
		return !thisCode.Equals(other);
	}
}
