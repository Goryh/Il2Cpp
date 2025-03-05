using System;
using System.Collections.Generic;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.CFG;

namespace Unity.IL2CPP;

internal class Labeler
{
	public readonly struct LabelId : IEquatable<LabelId>
	{
		public readonly int Offset;

		public readonly int TryBlockDepth;

		public LabelId(int offset, int tryBlockDepth)
		{
			Offset = offset;
			TryBlockDepth = tryBlockDepth;
		}

		public bool Equals(LabelId other)
		{
			if (Offset == other.Offset)
			{
				return TryBlockDepth == other.TryBlockDepth;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LabelId other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCodeHelper.Combine(Offset, TryBlockDepth);
		}
	}

	private readonly MethodDefinition _methodDefinition;

	private readonly Dictionary<Instruction, List<Instruction>> _jumpMap = new Dictionary<Instruction, List<Instruction>>();

	public Labeler(MethodDefinition methodDefinition)
	{
		_methodDefinition = methodDefinition;
		BuildLabelMap(methodDefinition);
	}

	public bool NeedsLabel(Instruction ins)
	{
		return _jumpMap.ContainsKey(ins);
	}

	public string ForJump(Instruction targetInstruction, Node currentNode)
	{
		return "goto " + FormatOffset(targetInstruction, currentNode) + ";";
	}

	public string ForLabel(Instruction ins, Node currentNode)
	{
		return FormatOffset(ins, currentNode) + ":";
	}

	public LabelId LabelIdForTarget(Instruction ins, Node currentNode)
	{
		return new LabelId(ins.Offset, currentNode.TryBlockDepth);
	}

	private void BuildLabelMap(MethodDefinition methodDefinition)
	{
		foreach (Instruction ins in methodDefinition.Body.Instructions)
		{
			if (ins.Operand is Instruction targetInstruction)
			{
				AddJumpLabel(ins, targetInstruction);
			}
			else if (ins.Operand is Instruction[] targetInstructions)
			{
				Instruction[] array = targetInstructions;
				foreach (Instruction switchTargetInstruction in array)
				{
					AddJumpLabel(ins, switchTargetInstruction);
				}
			}
		}
		foreach (ExceptionHandler handler in methodDefinition.Body.ExceptionHandlers)
		{
			AddJumpLabel(null, handler.HandlerStart);
		}
	}

	private void AddJumpLabel(Instruction ins, Instruction targetInstruction)
	{
		if (!_jumpMap.TryGetValue(targetInstruction, out var instructions))
		{
			_jumpMap.Add(targetInstruction, instructions = new List<Instruction>());
		}
		instructions.Add(ins);
	}

	private string FormatOffset(Instruction ins, Node currentNode)
	{
		return FormatOffset(ins.Offset, currentNode);
	}

	private string FormatOffset(int offset, Node currentNode)
	{
		string prefix = "IL";
		foreach (ExceptionHandler exceptionHandler in _methodDefinition.Body.ExceptionHandlers)
		{
			if (exceptionHandler.HandlerStart.Offset == offset)
			{
				switch (exceptionHandler.HandlerType)
				{
				case ExceptionHandlerType.Catch:
					prefix = "CATCH";
					break;
				case ExceptionHandlerType.Filter:
					prefix = "FILTER";
					break;
				case ExceptionHandlerType.Finally:
					prefix = "FINALLY";
					break;
				case ExceptionHandlerType.Fault:
					prefix = "FAULT";
					break;
				}
			}
		}
		int tryBlockDepth = currentNode.TryBlockDepth;
		if (tryBlockDepth > 0)
		{
			return $"{prefix}_{offset.ToString("x4")}_{tryBlockDepth.ToString()}";
		}
		return prefix + "_" + offset.ToString("x4");
	}
}
