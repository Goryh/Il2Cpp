using System;
using System.Text;
using Mono.Cecil.Cil;
using Unity.IL2CPP.DataModel.Modify.Definitions;

namespace Unity.IL2CPP.DataModel;

public class Instruction : IInstructionUpdater
{
	private Instruction _next;

	private Instruction _previous;

	private SequencePoint _sequencePoint;

	private readonly int _size;

	private object _operand;

	public object Operand => _operand;

	public OpCode OpCode { get; }

	public int Offset { get; internal set; }

	public Instruction Next => _next;

	public Instruction Previous => _previous;

	public SequencePoint SequencePoint => _sequencePoint;

	internal Instruction(Mono.Cecil.Cil.Instruction instruction)
	{
		OpCode = OpCodes.TranslateOpCode(instruction.OpCode);
		_operand = instruction.Operand;
		Offset = instruction.Offset;
		_size = instruction.GetSize();
	}

	internal Instruction(OpCode opCode, object operand)
	{
		_operand = operand;
		OpCode = opCode;
		_size = ComputeSize(opCode, operand);
	}

	public int GetSize()
	{
		return _size;
	}

	public static Instruction Create(OpCode opcode)
	{
		if (opcode.OperandType != OperandType.InlineNone)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, null);
	}

	public static Instruction Create(OpCode opcode, object operand)
	{
		return new Instruction(opcode, operand);
	}

	public static Instruction Create(OpCode opcode, TypeReference type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (opcode.OperandType != OperandType.InlineType && opcode.OperandType != OperandType.InlineTok)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, type);
	}

	public static Instruction Create(OpCode opcode, CallSite site)
	{
		if (site == null)
		{
			throw new ArgumentNullException("site");
		}
		if (opcode.Code != Code.Calli)
		{
			throw new ArgumentException("code");
		}
		return new Instruction(opcode, site);
	}

	public static Instruction Create(OpCode opcode, MethodReference method)
	{
		if (method == null)
		{
			throw new ArgumentNullException("method");
		}
		if (opcode.OperandType != OperandType.InlineMethod && opcode.OperandType != OperandType.InlineTok)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, method);
	}

	public static Instruction Create(OpCode opcode, FieldReference field)
	{
		if (field == null)
		{
			throw new ArgumentNullException("field");
		}
		if (opcode.OperandType != OperandType.InlineField && opcode.OperandType != OperandType.InlineTok)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, field);
	}

	public static Instruction Create(OpCode opcode, string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (opcode.OperandType != OperandType.InlineString)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, value);
	}

	public static Instruction Create(OpCode opcode, sbyte value)
	{
		if (opcode.OperandType != OperandType.ShortInlineI && opcode != OpCodes.Ldc_I4_S)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, value);
	}

	public static Instruction Create(OpCode opcode, byte value)
	{
		if (opcode.OperandType != OperandType.ShortInlineI || opcode == OpCodes.Ldc_I4_S)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, value);
	}

	public static Instruction Create(OpCode opcode, int value)
	{
		if (opcode.OperandType != OperandType.InlineI)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, value);
	}

	public static Instruction Create(OpCode opcode, long value)
	{
		if (opcode.OperandType != OperandType.InlineI8)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, value);
	}

	public static Instruction Create(OpCode opcode, float value)
	{
		if (opcode.OperandType != OperandType.ShortInlineR)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, value);
	}

	public static Instruction Create(OpCode opcode, double value)
	{
		if (opcode.OperandType != OperandType.InlineR)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, value);
	}

	public static Instruction Create(OpCode opcode, Instruction target)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		if (opcode.OperandType != 0 && opcode.OperandType != OperandType.ShortInlineBrTarget)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, target);
	}

	public static Instruction Create(OpCode opcode, Instruction[] targets)
	{
		if (targets == null)
		{
			throw new ArgumentNullException("targets");
		}
		if (opcode.OperandType != OperandType.InlineSwitch)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, targets);
	}

	public static Instruction Create(OpCode opcode, VariableDefinition variable)
	{
		if (variable == null)
		{
			throw new ArgumentNullException("variable");
		}
		if (opcode.OperandType != OperandType.ShortInlineVar && opcode.OperandType != OperandType.InlineVar)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, variable);
	}

	public static Instruction Create(OpCode opcode, ParameterDefinition parameter)
	{
		if (parameter == null)
		{
			throw new ArgumentNullException("parameter");
		}
		if (opcode.OperandType != OperandType.ShortInlineArg && opcode.OperandType != OperandType.InlineArg)
		{
			throw new ArgumentException("opcode");
		}
		return new Instruction(opcode, parameter);
	}

	internal void InitializeOperandDef(TypeReference typeReference)
	{
		_operand = typeReference;
	}

	internal void InitializeOperandDef(FieldReference fieldReference)
	{
		_operand = fieldReference;
	}

	internal void InitializeOperandDef(MethodReference methodReference)
	{
		_operand = methodReference;
	}

	internal void InitializeOperandDef(ParameterDefinition typeReference)
	{
		_operand = typeReference;
	}

	internal void InitializeOperandDef(VariableDefinition typeReference)
	{
		_operand = typeReference;
	}

	internal void InitializeOperandDef(CallSite callSite)
	{
		_operand = callSite;
	}

	internal void InitializeOperandDef(Instruction instruction)
	{
		_operand = instruction;
	}

	internal void InitializeOperandDef(Instruction[] instructions)
	{
		_operand = instructions;
	}

	internal void InitializeNextAndPrevious(Instruction next, Instruction previous)
	{
		_next = next;
		_previous = previous;
	}

	internal void InitializeSequencePoint(SequencePoint sequencePoint)
	{
		_sequencePoint = sequencePoint;
	}

	public override string ToString()
	{
		StringBuilder instruction = new StringBuilder();
		AppendLabel(instruction, this);
		instruction.Append(':');
		instruction.Append(' ');
		instruction.Append(OpCode.Name);
		if (_operand == null)
		{
			return instruction.ToString();
		}
		instruction.Append(' ');
		object operand = Operand;
		if (!(operand is Instruction instructionDef))
		{
			if (!(operand is Instruction[] instructionList))
			{
				if (operand is string str)
				{
					instruction.Append('"');
					instruction.Append(str);
					instruction.Append('"');
				}
				else
				{
					instruction.Append(Operand);
				}
			}
			else
			{
				for (int i = 0; i < instructionList.Length; i++)
				{
					if (i > 0)
					{
						instruction.Append(',');
					}
					AppendLabel(instruction, instructionList[i]);
				}
			}
		}
		else
		{
			AppendLabel(instruction, instructionDef);
		}
		return instruction.ToString();
	}

	void IInstructionUpdater.UpdateOperand(object operand)
	{
		_operand = operand;
	}

	private static void AppendLabel(StringBuilder instruction, Instruction instructionDef)
	{
		instruction.Append("IL_");
		instruction.Append(instructionDef.Offset.ToString("x4"));
	}

	private static int ComputeSize(OpCode opcode, object operand)
	{
		int size = opcode.Size;
		switch (opcode.OperandType)
		{
		case OperandType.InlineSwitch:
			return size + (1 + ((Instruction[])operand).Length) * 4;
		case OperandType.InlineI8:
		case OperandType.InlineR:
			return size + 8;
		case OperandType.InlineBrTarget:
		case OperandType.InlineField:
		case OperandType.InlineI:
		case OperandType.InlineMethod:
		case OperandType.InlineSig:
		case OperandType.InlineString:
		case OperandType.InlineTok:
		case OperandType.InlineType:
		case OperandType.ShortInlineR:
			return size + 4;
		case OperandType.InlineVar:
		case OperandType.InlineArg:
			return size + 2;
		case OperandType.ShortInlineBrTarget:
		case OperandType.ShortInlineI:
		case OperandType.ShortInlineVar:
		case OperandType.ShortInlineArg:
			return size + 1;
		default:
			return size;
		}
	}
}
