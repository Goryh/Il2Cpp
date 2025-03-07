using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Unity.IL2CPP.DataModel.Awesome.CFG;

public class TryCatchTreeBuilder
{
	internal enum ContextType
	{
		Root,
		Block,
		Try,
		Filter,
		Catch,
		Finally,
		Fault
	}

	internal class Context
	{
		public ContextType Type;

		public InstructionBlock Block;

		public ExceptionHandler Handler;

		public List<Context> Children = new List<Context>();
	}

	public const int EndOfMethodOffset = -1;

	private readonly MethodBody _methodBody;

	private readonly ReadOnlyCollection<InstructionBlock> _blocks;

	private readonly Stack<Context> _contextStack = new Stack<Context>();

	private readonly LazyDictionary<int, TryCatchInfo> _tryCatchInfos;

	private int _nextId;

	internal TryCatchTreeBuilder(MethodBody methodBody, ReadOnlyCollection<InstructionBlock> blocks, LazyDictionary<int, TryCatchInfo> tryCatchInfos)
	{
		_methodBody = methodBody;
		_blocks = blocks;
		_tryCatchInfos = tryCatchInfos;
	}

	internal Node Build()
	{
		Node node = (_methodBody.HasExceptionHandlers ? BuildTreeWithExceptionHandlers() : BuildTreeWithNoExceptionHandlers());
		if (_methodBody.HasExceptionHandlers && _methodBody.Method.DeclaringType.Context.Parameters.EnableDebugger)
		{
			AssignIds(node);
		}
		return node;
	}

	private void AssignIds(Node node)
	{
		if (node.Type == NodeType.Try && node.Id < 0)
		{
			node.Id = _nextId;
			Node[] catchNodes = node.CatchNodes;
			for (int i = 0; i < catchNodes.Length; i++)
			{
				catchNodes[i].Id = _nextId;
			}
			Node finallyNode = node.FinallyNode;
			if (finallyNode != null)
			{
				finallyNode.Id = _nextId;
			}
			_nextId++;
		}
		foreach (Node child in node.Children)
		{
			AssignIds(child);
		}
	}

	private Node BuildTreeWithNoExceptionHandlers()
	{
		int index = 0;
		Node[] children = new Node[_blocks.Count];
		foreach (InstructionBlock block in _blocks)
		{
			children[index++] = new Node(NodeType.Block, block);
		}
		return MakeRoot(children);
	}

	private Node BuildTreeWithExceptionHandlers()
	{
		_contextStack.Push(new Context
		{
			Type = ContextType.Root
		});
		foreach (InstructionBlock block in _blocks)
		{
			Instruction firstInstr = block.First;
			ProcessTryCatchInfo(_tryCatchInfos[firstInstr.Offset], firstInstr, block);
			_contextStack.Peek().Children.Add(new Context
			{
				Type = ContextType.Block,
				Block = block
			});
		}
		if (_tryCatchInfos.TryGetValue(-1, out var tryCatchInfo))
		{
			ProcessTryCatchInfo(tryCatchInfo, null, null);
		}
		if (_contextStack.Count > 1)
		{
			throw new NotSupportedException("Mismatched context depth when building try/catch tree!");
		}
		return MergeAndBuildRootNode(_contextStack.Pop());
	}

	private void ProcessTryCatchInfo(TryCatchInfo tryCatchInfo, Instruction firstInstr, InstructionBlock block)
	{
		if (tryCatchInfo.CatchStart != 0 && tryCatchInfo.FinallyStart != 0)
		{
			throw new NotSupportedException("An instruction cannot start both a catch and a finally block!");
		}
		for (int i = 0; i < tryCatchInfo.FinallyEnd; i++)
		{
			Context currentContext = _contextStack.Pop();
			_contextStack.Peek().Children.Add(new Context
			{
				Type = ContextType.Finally,
				Children = currentContext.Children,
				Handler = currentContext.Handler
			});
		}
		for (int j = 0; j < tryCatchInfo.FaultEnd; j++)
		{
			Context currentContext2 = _contextStack.Pop();
			_contextStack.Peek().Children.Add(new Context
			{
				Type = ContextType.Fault,
				Children = currentContext2.Children,
				Handler = currentContext2.Handler
			});
		}
		for (int k = 0; k < tryCatchInfo.FilterEnd; k++)
		{
			Context currentContext3 = _contextStack.Pop();
			_contextStack.Peek().Children.Add(new Context
			{
				Type = ContextType.Filter,
				Children = currentContext3.Children,
				Handler = currentContext3.Handler
			});
		}
		for (int l = 0; l < tryCatchInfo.CatchEnd; l++)
		{
			Context currentContext4 = _contextStack.Pop();
			_contextStack.Peek().Children.Add(new Context
			{
				Type = ContextType.Catch,
				Children = currentContext4.Children,
				Handler = currentContext4.Handler
			});
		}
		for (int m = 0; m < tryCatchInfo.TryEnd; m++)
		{
			Context currentContext5 = _contextStack.Pop();
			_contextStack.Peek().Children.Add(new Context
			{
				Type = ContextType.Try,
				Children = currentContext5.Children
			});
		}
		for (int n = 0; n < tryCatchInfo.FinallyStart; n++)
		{
			_contextStack.Push(new Context
			{
				Type = ContextType.Finally,
				Handler = _methodBody.ExceptionHandlers.Single((ExceptionHandler h) => h.HandlerType == ExceptionHandlerType.Finally && h.HandlerStart == firstInstr)
			});
		}
		for (int num = 0; num < tryCatchInfo.FaultStart; num++)
		{
			_contextStack.Push(new Context
			{
				Type = ContextType.Fault,
				Handler = _methodBody.ExceptionHandlers.Single((ExceptionHandler h) => h.HandlerType == ExceptionHandlerType.Fault && h.HandlerStart == firstInstr)
			});
		}
		for (int num2 = 0; num2 < tryCatchInfo.FilterStart; num2++)
		{
			_contextStack.Push(new Context
			{
				Type = ContextType.Filter,
				Handler = _methodBody.ExceptionHandlers.Single((ExceptionHandler h) => h.HandlerType == ExceptionHandlerType.Filter && h.FilterStart == firstInstr)
			});
		}
		for (int num3 = 0; num3 < tryCatchInfo.CatchStart; num3++)
		{
			_contextStack.Push(new Context
			{
				Type = ContextType.Catch,
				Handler = _methodBody.ExceptionHandlers.Single((ExceptionHandler h) => (h.HandlerType == ExceptionHandlerType.Catch || h.HandlerType == ExceptionHandlerType.Filter) && h.HandlerStart == firstInstr)
			});
		}
		for (int num4 = 0; num4 < tryCatchInfo.TryStart; num4++)
		{
			_contextStack.Push(new Context
			{
				Type = ContextType.Try
			});
		}
	}

	private static Node MergeAndBuildRootNode(Context context)
	{
		int index = 0;
		Node[] children = new Node[context.Children.Count];
		foreach (Context child in context.Children)
		{
			children[index++] = MergeAndBuildRootNodeRecursive(child);
		}
		return MakeRoot(children);
	}

	private static Node MergeAndBuildRootNodeRecursive(Context context)
	{
		int index = 0;
		Node[] children = new Node[context.Children.Count];
		foreach (Context child in context.Children)
		{
			children[index++] = MergeAndBuildRootNodeRecursive(child);
		}
		if (children.Length == 1)
		{
			Node firstChild = children[0];
			if (firstChild.Type == NodeType.Block)
			{
				return new Node(null, NodeTypeFor(context), firstChild.Block, new Node[0], ExceptionHandlerFor(context));
			}
		}
		return new Node(null, NodeTypeFor(context), BlockFor(context), children, ExceptionHandlerFor(context));
	}

	private static NodeType NodeTypeFor(Context context)
	{
		return context.Type switch
		{
			ContextType.Root => NodeType.Root, 
			ContextType.Try => NodeType.Try, 
			ContextType.Catch => NodeType.Catch, 
			ContextType.Filter => NodeType.Filter, 
			ContextType.Finally => NodeType.Finally, 
			ContextType.Fault => NodeType.Fault, 
			_ => NodeType.Block, 
		};
	}

	private static InstructionBlock BlockFor(Context context)
	{
		if (context.Type != ContextType.Block)
		{
			return null;
		}
		return context.Block;
	}

	private static ExceptionHandler ExceptionHandlerFor(Context context)
	{
		ContextType type = context.Type;
		if ((uint)(type - 3) <= 3u)
		{
			return context.Handler;
		}
		return null;
	}

	private static Node MakeRoot(Node[] children)
	{
		return new Node(null, NodeType.Root, null, children, null);
	}
}
