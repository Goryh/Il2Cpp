using System;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP.DataModel.Awesome.CFG;

public class ControlFlowGraph
{
	private readonly MethodBody body;

	private readonly ReadOnlyCollection<InstructionBlock> blocks;

	private readonly Node _flowTree;

	public MethodBody MethodBody => body;

	public ReadOnlyCollection<InstructionBlock> Blocks => blocks;

	public Node FlowTree => _flowTree;

	public ControlFlowGraph(MethodBody body, ReadOnlyCollection<InstructionBlock> blocks, Node flowTree)
	{
		this.body = body;
		this.blocks = blocks;
		_flowTree = flowTree;
	}

	public static ReadOnlyCollection<InstructionBlock> CreateBasicBlocks(MethodDefinition method)
	{
		if (method == null)
		{
			throw new ArgumentNullException("method");
		}
		if (!method.HasBody)
		{
			throw new ArgumentException();
		}
		return new ControlFlowGraphBuilder(method).BuildBasicBlocks();
	}

	public static ControlFlowGraph Create(MethodDefinition method)
	{
		return Create(method, CreateBasicBlocks(method));
	}

	public static ControlFlowGraph Create(MethodDefinition method, ReadOnlyCollection<InstructionBlock> blocks)
	{
		return ControlFlowGraphBuilder.CreateGraph(method.Body, blocks);
	}
}
