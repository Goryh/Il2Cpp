using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.Awesome.CFG;

public class ControlFlowGraphBuilder
{
	private class IntermediateInstructionData
	{
		public InstructionBlock block;

		public int stackHeight;

		public IntermediateInstructionData(int stackHeight_, InstructionBlock block_)
		{
			stackHeight = stackHeight_;
			block = block_;
		}
	}

	private MethodBody body;

	private Dictionary<int, InstructionData> data;

	private Dictionary<int, InstructionBlock> blocks = new Dictionary<int, InstructionBlock>();

	private HashSet<int> exception_objects_offsets;

	internal ControlFlowGraphBuilder(MethodDefinition method)
	{
		body = method.Body;
		if (body.ExceptionHandlers.Count > 0)
		{
			exception_objects_offsets = new HashSet<int>();
		}
	}

	public ReadOnlyCollection<InstructionBlock> BuildBasicBlocks()
	{
		DelimitBlocks();
		ConnectBlocks();
		ComputeInstructionData();
		return ToArray().AsReadOnly();
	}

	public static ControlFlowGraph CreateGraph(MethodBody body, ReadOnlyCollection<InstructionBlock> blocks)
	{
		MarkBlocksDeadIfNeeded(blocks);
		Node node = new TryCatchTreeBuilder(body, blocks, TryCatchInfoCollector.Collect(body)).Build();
		return new ControlFlowGraph(body, blocks, node);
	}

	public static void MarkBlocksDeadIfNeeded(ControlFlowGraph cfg)
	{
		MarkBlocksDeadIfNeeded(cfg.Blocks);
	}

	private static void MarkBlocksDeadIfNeeded(IReadOnlyList<InstructionBlock> instructionBlocks)
	{
		if (instructionBlocks.Count == 1)
		{
			return;
		}
		foreach (InstructionBlock instructionBlock in instructionBlocks)
		{
			instructionBlock.MarkIsDead();
		}
		instructionBlocks[0].MarkIsAliveRecursive();
	}

	private void DelimitBlocks()
	{
		ReadOnlyCollection<Instruction> instructions = body.Instructions;
		MarkBlockStarts(instructions);
		ReadOnlyCollection<ExceptionHandler> exceptions = body.ExceptionHandlers;
		MarkBlockStarts(exceptions);
		MarkBlockEnds(instructions);
	}

	private void MarkBlockStarts(IList<ExceptionHandler> handlers)
	{
		for (int i = 0; i < handlers.Count; i++)
		{
			ExceptionHandler handler = handlers[i];
			MarkBlockStart(handler.TryStart);
			MarkBlockStart(handler.HandlerStart);
			if (handler.HandlerType == ExceptionHandlerType.Filter)
			{
				MarkExceptionObjectPosition(handler.FilterStart);
				MarkBlockStart(handler.FilterStart);
			}
			else if (handler.HandlerType == ExceptionHandlerType.Catch)
			{
				MarkExceptionObjectPosition(handler.HandlerStart);
				if (handler.HandlerEnd != null)
				{
					MarkBlockStart(handler.HandlerEnd);
				}
			}
		}
	}

	private void MarkExceptionObjectPosition(Instruction instruction)
	{
		exception_objects_offsets.Add(instruction.Offset);
	}

	private void MarkBlockStarts(IList<Instruction> instructions)
	{
		for (int i = 0; i < instructions.Count; i++)
		{
			Instruction instruction = instructions[i];
			if (i == 0)
			{
				MarkBlockStart(instruction);
			}
			if (!IsBlockDelimiter(instruction))
			{
				continue;
			}
			if (HasMultipleBranches(instruction))
			{
				Instruction[] branchTargets = GetBranchTargets(instruction);
				foreach (Instruction target in branchTargets)
				{
					if (target != null)
					{
						MarkBlockStart(target);
					}
				}
			}
			else
			{
				Instruction target2 = GetBranchTarget(instruction);
				if (target2 != null)
				{
					MarkBlockStart(target2);
				}
			}
			if (instruction.Next != null)
			{
				MarkBlockStart(instruction.Next);
			}
		}
	}

	private void MarkBlockEnds(IList<Instruction> instructions)
	{
		InstructionBlock[] blocks = ToArray();
		InstructionBlock current = blocks[0];
		for (int i = 1; i < blocks.Length; i++)
		{
			InstructionBlock block = blocks[i];
			current.Last = block.First.Previous;
			current = block;
		}
		current.Last = instructions[instructions.Count - 1];
	}

	private static bool IsBlockDelimiter(Instruction instruction)
	{
		switch (instruction.OpCode.FlowControl)
		{
		case FlowControl.Branch:
		case FlowControl.Break:
		case FlowControl.Cond_Branch:
		case FlowControl.Return:
		case FlowControl.Throw:
			return true;
		default:
			return false;
		}
	}

	private void MarkBlockStart(Instruction instruction)
	{
		InstructionBlock block = GetBlock(instruction);
		if (block == null)
		{
			block = new InstructionBlock(instruction);
			RegisterBlock(block);
		}
	}

	private void ComputeInstructionData()
	{
		data = new Dictionary<int, InstructionData>();
		HashSet<InstructionBlock> visited = new HashSet<InstructionBlock>();
		foreach (InstructionBlock block in blocks.Values)
		{
			ComputeInstructionData(visited, 0, block);
		}
	}

	private void ComputeInstructionData(HashSet<InstructionBlock> visited, int stackHeight, InstructionBlock block)
	{
		Stack<IntermediateInstructionData> stack = new Stack<IntermediateInstructionData>();
		stack.Push(new IntermediateInstructionData(stackHeight, block));
		while (stack.Count > 0)
		{
			IntermediateInstructionData instData = stack.Pop();
			if (visited.Contains(instData.block))
			{
				continue;
			}
			visited.Add(instData.block);
			foreach (Instruction instruction in instData.block)
			{
				instData.stackHeight = ComputeInstructionData(instData.stackHeight, instruction);
			}
			foreach (InstructionBlock successor in instData.block.Successors)
			{
				stack.Push(new IntermediateInstructionData(instData.stackHeight, successor));
			}
		}
	}

	private bool IsCatchStart(Instruction instruction)
	{
		if (exception_objects_offsets == null)
		{
			return false;
		}
		return exception_objects_offsets.Contains(instruction.Offset);
	}

	private int ComputeInstructionData(int stackHeight, Instruction instruction)
	{
		if (IsCatchStart(instruction))
		{
			stackHeight++;
		}
		int before = stackHeight;
		int after = ComputeNewStackHeight(stackHeight, instruction);
		data.Add(instruction.Offset, new InstructionData(before, after));
		return after;
	}

	private int ComputeNewStackHeight(int stackHeight, Instruction instruction)
	{
		return stackHeight + GetPushDelta(instruction) - GetPopDelta(stackHeight, instruction);
	}

	private static int GetPushDelta(Instruction instruction)
	{
		OpCode code = instruction.OpCode;
		switch (code.StackBehaviourPush)
		{
		case StackBehaviour.Push0:
			return 0;
		case StackBehaviour.Push1:
		case StackBehaviour.Pushi:
		case StackBehaviour.Pushi8:
		case StackBehaviour.Pushr4:
		case StackBehaviour.Pushr8:
		case StackBehaviour.Pushref:
			return 1;
		case StackBehaviour.Push1_push1:
			return 2;
		case StackBehaviour.Varpush:
			if (code.FlowControl == FlowControl.Call)
			{
				return (!((IMethodSignature)instruction.Operand).ReturnType.IsVoid) ? 1 : 0;
			}
			break;
		}
		throw new ArgumentException(Formatter.FormatInstruction(instruction));
	}

	private int GetPopDelta(int stackHeight, Instruction instruction)
	{
		OpCode code = instruction.OpCode;
		switch (code.StackBehaviourPop)
		{
		case StackBehaviour.Pop0:
			return 0;
		case StackBehaviour.Pop1:
		case StackBehaviour.Popi:
		case StackBehaviour.Popref:
			return 1;
		case StackBehaviour.Pop1_pop1:
		case StackBehaviour.Popi_pop1:
		case StackBehaviour.Popi_popi:
		case StackBehaviour.Popi_popi8:
		case StackBehaviour.Popi_popr4:
		case StackBehaviour.Popi_popr8:
		case StackBehaviour.Popref_pop1:
		case StackBehaviour.Popref_popi:
			return 2;
		case StackBehaviour.Popi_popi_popi:
		case StackBehaviour.Popref_popi_popi:
		case StackBehaviour.Popref_popi_popi8:
		case StackBehaviour.Popref_popi_popr4:
		case StackBehaviour.Popref_popi_popr8:
		case StackBehaviour.Popref_popi_popref:
			return 3;
		case StackBehaviour.PopAll:
			return stackHeight;
		case StackBehaviour.Varpop:
			if (code.FlowControl == FlowControl.Call)
			{
				IMethodSignature obj = (IMethodSignature)instruction.Operand;
				int count = obj.Parameters.Count;
				if (obj.HasThis && OpCodes.Newobj.Code == code.Code)
				{
					count++;
				}
				return count;
			}
			if (code.Code == Code.Ret)
			{
				return (!IsVoidMethod()) ? 1 : 0;
			}
			break;
		}
		throw new ArgumentException(Formatter.FormatInstruction(instruction));
	}

	private bool IsVoidMethod()
	{
		return body.Method.ReturnType.IsVoid;
	}

	private InstructionBlock[] ToArray()
	{
		InstructionBlock[] result = new InstructionBlock[blocks.Count];
		blocks.Values.CopyTo(result, 0);
		Array.Sort(result);
		ComputeIndexes(result);
		return result;
	}

	private static void ComputeIndexes(InstructionBlock[] blocks)
	{
		for (int i = 0; i < blocks.Length; i++)
		{
			blocks[i].Index = i;
		}
	}

	private void ConnectBlocks()
	{
		foreach (InstructionBlock block in blocks.Values)
		{
			ConnectBlock(block);
		}
	}

	private void ConnectBlock(InstructionBlock block)
	{
		if (block.Last == null)
		{
			throw new ArgumentException("Undelimited block at offset " + block.First.Offset);
		}
		Instruction instruction = block.Last;
		switch (instruction.OpCode.FlowControl)
		{
		case FlowControl.Branch:
		case FlowControl.Cond_Branch:
			if (HasMultipleBranches(instruction))
			{
				InstructionBlock[] blocks = GetBranchTargetsBlocks(instruction);
				InstructionBlock[] array = blocks;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].IsBranchTarget = true;
				}
				if (instruction.Next != null)
				{
					blocks = AddBlock(GetBlock(instruction.Next), blocks);
				}
				block.AddSuccessors(blocks);
			}
			else
			{
				InstructionBlock target = GetBranchTargetBlock(instruction);
				target.IsBranchTarget = true;
				if (instruction.OpCode.FlowControl == FlowControl.Cond_Branch && instruction.Next != null)
				{
					block.AddSuccessors(new InstructionBlock[2]
					{
						target,
						GetBlock(instruction.Next)
					});
				}
				else
				{
					block.AddSuccessors(new InstructionBlock[1] { target });
				}
			}
			break;
		case FlowControl.Break:
		case FlowControl.Call:
		case FlowControl.Next:
			if (instruction.Next != null)
			{
				block.AddSuccessors(new InstructionBlock[1] { GetBlock(instruction.Next) });
			}
			break;
		default:
			throw new NotSupportedException($"Unhandled instruction flow behavior {instruction.OpCode.FlowControl}: {Formatter.FormatInstruction(instruction)}");
		case FlowControl.Return:
		case FlowControl.Throw:
			break;
		}
	}

	private static InstructionBlock[] AddBlock(InstructionBlock block, InstructionBlock[] blocks)
	{
		InstructionBlock[] result = new InstructionBlock[blocks.Length + 1];
		Array.Copy(blocks, result, blocks.Length);
		result[^1] = block;
		return result;
	}

	private static bool HasMultipleBranches(Instruction instruction)
	{
		return instruction.OpCode.Code == Code.Switch;
	}

	private InstructionBlock[] GetBranchTargetsBlocks(Instruction instruction)
	{
		Instruction[] targets = GetBranchTargets(instruction);
		InstructionBlock[] blocks = new InstructionBlock[targets.Length];
		for (int i = 0; i < targets.Length; i++)
		{
			blocks[i] = GetBlock(targets[i]);
		}
		return blocks;
	}

	private static Instruction[] GetBranchTargets(Instruction instruction)
	{
		return (Instruction[])instruction.Operand;
	}

	private InstructionBlock GetBranchTargetBlock(Instruction instruction)
	{
		return GetBlock(GetBranchTarget(instruction));
	}

	private static Instruction GetBranchTarget(Instruction instruction)
	{
		return (Instruction)instruction.Operand;
	}

	private void RegisterBlock(InstructionBlock block)
	{
		blocks.Add(block.First.Offset, block);
	}

	private InstructionBlock GetBlock(Instruction firstInstruction)
	{
		blocks.TryGetValue(firstInstruction.Offset, out var block);
		return block;
	}
}
