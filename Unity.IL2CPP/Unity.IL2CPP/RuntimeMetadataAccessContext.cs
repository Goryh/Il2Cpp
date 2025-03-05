using System;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.CFG;

namespace Unity.IL2CPP;

public static class RuntimeMetadataAccessContext
{
	private class NopContext : IDisposable
	{
		public void Dispose()
		{
		}
	}

	private class InitMetadataInlineContext : IDisposable
	{
		private readonly IRuntimeMetadataAccess _runtimeMetadataAccess;

		public InitMetadataInlineContext(IRuntimeMetadataAccess runtimeMetadataAccess)
		{
			_runtimeMetadataAccess = runtimeMetadataAccess;
			_runtimeMetadataAccess.StartInitMetadataInline();
		}

		public void Dispose()
		{
			_runtimeMetadataAccess.EndInitMetadataInline();
		}
	}

	private static readonly NopContext Nop = new NopContext();

	private const int MaxPathSearchLength = 4;

	public static IDisposable Create(IRuntimeMetadataAccess runtimeMetadataAccess, Node node)
	{
		if (node.Type == NodeType.Catch || node.Type == NodeType.Fault || node.Type == NodeType.Filter || IsExceptionPath(node.Block, 0))
		{
			return new InitMetadataInlineContext(runtimeMetadataAccess);
		}
		return Nop;
	}

	public static IDisposable CreateForCatchHandlers(IRuntimeMetadataAccess runtimeMetadataAccess)
	{
		return new InitMetadataInlineContext(runtimeMetadataAccess);
	}

	private static bool IsExceptionPath(InstructionBlock insBlock, int pathLength)
	{
		if (insBlock == null)
		{
			return false;
		}
		if (insBlock.Last.OpCode == OpCodes.Throw)
		{
			return true;
		}
		bool any = insBlock.Successors.Count > 0;
		if (pathLength > 4 || !any)
		{
			return false;
		}
		foreach (InstructionBlock successor in insBlock.Successors)
		{
			if (!IsExceptionPath(successor, pathLength + 1))
			{
				return false;
			}
		}
		return true;
	}
}
