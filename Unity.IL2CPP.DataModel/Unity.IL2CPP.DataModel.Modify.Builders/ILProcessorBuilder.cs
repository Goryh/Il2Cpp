using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.IL2CPP.DataModel.Modify.Builders;

public class ILProcessorBuilder
{
	private readonly List<Instruction> _instructions = new List<Instruction>();

	internal List<Instruction> Instructions => _instructions.ToList();

	public MethodDefinitionBuilder Method { get; }

	internal ILProcessorBuilder(MethodDefinitionBuilder methodBuilder)
	{
		Method = methodBuilder;
	}

	public Instruction Create(OpCode opcode)
	{
		return Instruction.Create(opcode);
	}

	public Instruction Create(OpCode opcode, object operand)
	{
		return Instruction.Create(opcode, operand);
	}

	public Instruction Create(OpCode opcode, TypeReference type)
	{
		return Instruction.Create(opcode, type);
	}

	public Instruction Create(OpCode opcode, CallSite site)
	{
		return Instruction.Create(opcode, site);
	}

	public Instruction Create(OpCode opcode, MethodReference method)
	{
		return Instruction.Create(opcode, method);
	}

	public Instruction Create(OpCode opcode, FieldReference field)
	{
		return Instruction.Create(opcode, field);
	}

	public Instruction Create(OpCode opcode, string value)
	{
		return Instruction.Create(opcode, value);
	}

	public Instruction Create(OpCode opcode, sbyte value)
	{
		return Instruction.Create(opcode, value);
	}

	public Instruction Create(OpCode opcode, byte value)
	{
		if (opcode.OperandType == OperandType.ShortInlineVar)
		{
			throw new NotImplementedException();
		}
		if (opcode.OperandType == OperandType.ShortInlineArg)
		{
			throw new NotImplementedException();
		}
		return Instruction.Create(opcode, value);
	}

	public Instruction Create(OpCode opcode, int value)
	{
		if (opcode.OperandType == OperandType.InlineVar)
		{
			throw new NotImplementedException();
		}
		if (opcode.OperandType == OperandType.InlineArg)
		{
			throw new NotImplementedException();
		}
		return Instruction.Create(opcode, value);
	}

	public Instruction Create(OpCode opcode, long value)
	{
		return Instruction.Create(opcode, value);
	}

	public Instruction Create(OpCode opcode, float value)
	{
		return Instruction.Create(opcode, value);
	}

	public Instruction Create(OpCode opcode, double value)
	{
		return Instruction.Create(opcode, value);
	}

	public Instruction Create(OpCode opcode, Instruction target)
	{
		return Instruction.Create(opcode, target);
	}

	public Instruction Create(OpCode opcode, Instruction[] targets)
	{
		return Instruction.Create(opcode, targets);
	}

	public Instruction Create(OpCode opcode, VariableDefinition variable)
	{
		return Instruction.Create(opcode, variable);
	}

	public Instruction Create(OpCode opcode, ParameterDefinition parameter)
	{
		return Instruction.Create(opcode, parameter);
	}

	public ILProcessorBuilder Emit(OpCode opcode)
	{
		Append(Create(opcode));
		return this;
	}

	public void Emit(OpCode opcode, object operand)
	{
		Append(Create(opcode, operand));
	}

	public void Emit(OpCode opcode, TypeReference type)
	{
		Append(Create(opcode, type));
	}

	public void Emit(OpCode opcode, MethodReference method)
	{
		Append(Create(opcode, method));
	}

	public void Emit(OpCode opcode, CallSite site)
	{
		Append(Create(opcode, site));
	}

	public void Emit(OpCode opcode, FieldReference field)
	{
		Append(Create(opcode, field));
	}

	public void Emit(OpCode opcode, string value)
	{
		Append(Create(opcode, value));
	}

	public void Emit(OpCode opcode, byte value)
	{
		Append(Create(opcode, value));
	}

	public void Emit(OpCode opcode, sbyte value)
	{
		Append(Create(opcode, value));
	}

	public void Emit(OpCode opcode, int value)
	{
		Append(Create(opcode, value));
	}

	public void Emit(OpCode opcode, long value)
	{
		Append(Create(opcode, value));
	}

	public void Emit(OpCode opcode, float value)
	{
		Append(Create(opcode, value));
	}

	public void Emit(OpCode opcode, double value)
	{
		Append(Create(opcode, value));
	}

	public void Emit(OpCode opcode, Instruction target)
	{
		Append(Create(opcode, target));
	}

	public void Emit(OpCode opcode, Instruction[] targets)
	{
		Append(Create(opcode, targets));
	}

	public void Emit(OpCode opcode, VariableDefinition variable)
	{
		Append(Create(opcode, variable));
	}

	public void Emit(OpCode opcode, ParameterDefinition parameter)
	{
		Append(Create(opcode, parameter));
	}

	public void Append(Instruction instruction)
	{
		if (instruction == null)
		{
			throw new ArgumentNullException("instruction");
		}
		_instructions.Add(instruction);
		if (_instructions.Count > 1)
		{
			Instruction previous = _instructions[_instructions.Count - 2];
			previous.InitializeNextAndPrevious(instruction, previous.Previous);
			instruction.InitializeNextAndPrevious(null, previous);
		}
	}
}
