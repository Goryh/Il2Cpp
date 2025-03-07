using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Unity.IL2CPP.DataModel.Awesome.CFG;

[DebuggerDisplay("{First},{Last}")]
public class InstructionBlock : IEnumerable<Instruction>, IEnumerable, IComparable<InstructionBlock>
{
	private int index;

	private Instruction first;

	private Instruction last;

	private Dictionary<InstructionBlock, int> _successors = new Dictionary<InstructionBlock, int>();

	private List<InstructionBlock> _exceptionSuccessors = new List<InstructionBlock>();

	public int Index
	{
		get
		{
			return index;
		}
		internal set
		{
			index = value;
		}
	}

	public Instruction First
	{
		get
		{
			return first;
		}
		internal set
		{
			first = value;
		}
	}

	public Instruction Last
	{
		get
		{
			return last;
		}
		internal set
		{
			last = value;
		}
	}

	public IReadOnlyCollection<InstructionBlock> Successors => _successors.Keys;

	public IReadOnlyList<InstructionBlock> ExceptionSuccessors => _exceptionSuccessors;

	public bool IsBranchTarget { get; set; }

	public bool IsDead { get; private set; }

	internal InstructionBlock(Instruction first)
	{
		if (first == null)
		{
			throw new ArgumentNullException("first");
		}
		this.first = first;
	}

	public int CompareTo(InstructionBlock block)
	{
		return first.Offset - block.First.Offset;
	}

	public IEnumerator<Instruction> GetEnumerator()
	{
		Instruction instruction = first;
		while (true)
		{
			yield return instruction;
			if (instruction == last)
			{
				break;
			}
			instruction = instruction.Next;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	internal void AddSuccessors(IEnumerable<InstructionBlock> blocks)
	{
		foreach (InstructionBlock block in blocks)
		{
			if (!_successors.TryAdd(block, 1))
			{
				_successors[block]++;
			}
		}
	}

	public void MarkSuccessorNotTaken(InstructionBlock block)
	{
		_successors[block]--;
	}

	internal void AddExceptionSuccessor(InstructionBlock block)
	{
		_exceptionSuccessors.Add(block);
	}

	public void MarkIsDead()
	{
		IsDead = true;
	}

	internal void MarkIsAliveRecursive()
	{
		Stack<InstructionBlock> stack = new Stack<InstructionBlock>();
		stack.Push(this);
		while (stack.Count > 0)
		{
			InstructionBlock block = stack.Pop();
			if (!block.IsDead)
			{
				continue;
			}
			block.IsDead = false;
			foreach (InstructionBlock successor in from s in block._successors
				where s.Value > 0
				select s.Key)
			{
				stack.Push(successor);
			}
			foreach (InstructionBlock exceptionSuccessor in block.ExceptionSuccessors)
			{
				stack.Push(exceptionSuccessor);
			}
		}
	}
}
