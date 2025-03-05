using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.CFG;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP;

public class ExceptionSupport
{
	public const string ActiveExceptions = "__active_exceptions";

	public const string LocalFilterName = "__filter_local";

	public const string FinallyBlockName = "__finallyBlock";

	private readonly ReadOnlyContext _context;

	private readonly Node _flowTree;

	private readonly IGeneratedMethodCodeWriter _writer;

	private readonly MethodBody _methodBody;

	private readonly Dictionary<Node, HashSet<Instruction>> _leaveTargets = new Dictionary<Node, HashSet<Instruction>>();

	public Node FlowTree => _flowTree;

	public string EmitGetActiveException(TypeReference exceptionType)
	{
		return "IL2CPP_GET_ACTIVE_EXCEPTION(" + exceptionType.CppNameForVariable + ")";
	}

	public string EmitPushActiveException(string exceptionExpression)
	{
		return "IL2CPP_PUSH_ACTIVE_EXCEPTION(" + exceptionExpression + ")";
	}

	public string EmitPopActiveException(TypeReference exceptionType)
	{
		return "IL2CPP_POP_ACTIVE_EXCEPTION(" + exceptionType.CppNameForVariable + ")";
	}

	public ExceptionSupport(ReadOnlyContext context, MethodDefinition methodDefinition, Node flowTree, IGeneratedMethodCodeWriter writer)
	{
		_context = context;
		_writer = writer;
		_methodBody = methodDefinition.Body;
		_flowTree = flowTree;
	}

	public void Prepare()
	{
		if (_methodBody.HasExceptionHandlers)
		{
			int maxTryDepth = MaxTryCatchDepth();
			if (maxTryDepth > 0)
			{
				IGeneratedMethodCodeWriter writer = _writer;
				writer.WriteLine($"il2cpp::utils::ExceptionSupportStack<{"RuntimeObject"}*, {maxTryDepth}> {"__active_exceptions"};");
			}
		}
	}

	private static bool HasFinallyOrFaultBlocks(MethodBody body)
	{
		if (body.HasExceptionHandlers)
		{
			return body.ExceptionHandlers.Any((ExceptionHandler e) => e.HandlerType == ExceptionHandlerType.Fault || e.HandlerType == ExceptionHandlerType.Finally);
		}
		return false;
	}

	private int MaxTryCatchDepth()
	{
		return MaxTryCatchDepth(_flowTree, 0);
	}

	internal static int MaxTryCatchDepth(Node node)
	{
		return MaxTryCatchDepth(node, 0);
	}

	private static int MaxTryCatchDepth(Node node, int depth)
	{
		switch (node.Type)
		{
		case NodeType.Try:
			if (node.HasCatchOrFilterNodes())
			{
				depth++;
			}
			break;
		case NodeType.Catch:
			depth++;
			break;
		}
		if (node.Children.Count == 0)
		{
			return depth;
		}
		return node.Children.Max((Node n) => MaxTryCatchDepth(n, depth));
	}

	internal StackInfo? GetActiveExceptionIfNeeded(Node node, TypeResolver typeResolver, TypeDefinition systemException)
	{
		if (node.Type == NodeType.Catch && node.Block != null)
		{
			return GetActiveException(typeResolver.Resolve(node.Handler.CatchType ?? systemException));
		}
		if (node.Type == NodeType.Filter && node.Block != null)
		{
			return GetActiveException(systemException);
		}
		if (node.Parent.Type == NodeType.Catch)
		{
			if (node.Parent.Children[0] != node)
			{
				return null;
			}
			return GetActiveException(typeResolver.Resolve(node.Parent.Handler.CatchType ?? systemException));
		}
		if (node.Parent.Type == NodeType.Filter && node.Parent.Handler.FilterStart == node.Start)
		{
			return GetActiveException(systemException);
		}
		return null;
	}

	internal IEnumerable<Instruction> LeaveTargetsFor(Node finallyNode)
	{
		if (!_leaveTargets.TryGetValue(finallyNode, out var targets))
		{
			yield break;
		}
		foreach (Instruction item in targets)
		{
			yield return item;
		}
	}

	internal void AddLeaveTarget(Node finallyNode, Instruction instruction)
	{
		if (!_leaveTargets.TryGetValue(finallyNode, out var targets))
		{
			targets = new HashSet<Instruction>();
			_leaveTargets[finallyNode] = targets;
		}
		targets.Add((Instruction)instruction.Operand);
	}

	private StackInfo GetActiveException(TypeReference catchType)
	{
		return new StackInfo($"(({catchType.CppNameForVariable}){EmitGetActiveException(catchType)})", ResolvedTypeInfo.FromResolvedType(catchType));
	}
}
