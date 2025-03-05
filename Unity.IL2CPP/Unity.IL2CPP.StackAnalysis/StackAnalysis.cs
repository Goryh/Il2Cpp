using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.CFG;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP.StackAnalysis;

public class StackAnalysis
{
	private readonly ReadOnlyContext _context;

	private readonly ResolvedTypeFactory _typeFactory;

	private readonly ResolvedMethodContext _resolvedMethodInfo;

	private readonly MethodDefinition _methodDefinition;

	private readonly Dictionary<InstructionBlock, StackState> _ins = new Dictionary<InstructionBlock, StackState>();

	private readonly Dictionary<InstructionBlock, StackState> _outs = new Dictionary<InstructionBlock, StackState>();

	private readonly Dictionary<InstructionBlock, GlobalVariable[]> _globalGlobalVariables = new Dictionary<InstructionBlock, GlobalVariable[]>();

	public IEnumerable<GlobalVariable> Globals
	{
		get
		{
			foreach (GlobalVariable[] blockVariables in _globalGlobalVariables.Values)
			{
				GlobalVariable[] array = blockVariables;
				for (int i = 0; i < array.Length; i++)
				{
					yield return array[i];
				}
			}
		}
	}

	public static StackAnalysis Analyze(MethodWriteContext context, ResolvedTypeFactory typeFactory, ResolvedMethodContext resolvedMethodInfo)
	{
		return Analyze(context, context.MethodDefinition, typeFactory, resolvedMethodInfo);
	}

	public static StackAnalysis Analyze(ReadOnlyContext context, MethodDefinition method, ResolvedTypeFactory typeFactory, ResolvedMethodContext resolvedMethodInfo)
	{
		StackAnalysis stackAnalysis = new StackAnalysis(context, method, typeFactory, resolvedMethodInfo);
		stackAnalysis.Analyze();
		return stackAnalysis;
	}

	private StackAnalysis(ReadOnlyContext context, MethodDefinition method, ResolvedTypeFactory typeFactory, ResolvedMethodContext resolvedMethodInfo)
	{
		_context = context;
		_methodDefinition = method;
		_typeFactory = typeFactory;
		_resolvedMethodInfo = resolvedMethodInfo;
	}

	private void Analyze()
	{
		foreach (ResolvedInstructionBlock block in _resolvedMethodInfo.Blocks)
		{
			if (!_ins.TryGetValue(block.Block, out var initialValue))
			{
				initialValue = new StackState();
			}
			StackState stackState = StackStateBuilder.StackStateFor(_context, _methodDefinition, _typeFactory, _resolvedMethodInfo, block, initialValue);
			_outs.Add(block.Block, stackState.Clone());
			foreach (InstructionBlock successor in block.Block.Successors)
			{
				if (!_ins.ContainsKey(successor))
				{
					_ins[successor] = new StackState();
				}
				_ins[successor].Merge(stackState);
			}
		}
		ComputeGlobalVariables();
	}

	public StackState InputStackStateFor(InstructionBlock block)
	{
		if (!_ins.TryGetValue(block, out var ins))
		{
			return new StackState();
		}
		return ins;
	}

	public StackState OutputStackStateFor(InstructionBlock block)
	{
		if (!_outs.TryGetValue(block, out var ins))
		{
			return new StackState();
		}
		return ins;
	}

	public GlobalVariable[] InputVariablesFor(InstructionBlock block)
	{
		if (_globalGlobalVariables.TryGetValue(block, out var globalVariables))
		{
			return globalVariables;
		}
		return Array.Empty<GlobalVariable>();
	}

	private void ComputeGlobalVariables()
	{
		foreach (KeyValuePair<InstructionBlock, StackState> pair in _ins.Where((KeyValuePair<InstructionBlock, StackState> e) => !e.Value.IsEmpty))
		{
			int index = 0;
			GlobalVariable[] items = new GlobalVariable[pair.Value.Entries.Count];
			foreach (Entry entry in pair.Value.Entries)
			{
				items[index] = new GlobalVariable
				{
					BlockIndex = pair.Key.Index,
					Index = index++,
					Type = ComputeType(entry)
				};
			}
			_globalGlobalVariables.Add(pair.Key, items);
		}
	}

	private ResolvedTypeInfo ComputeType(Entry entry)
	{
		if (entry.Types.Any((ResolvedTypeInfo t) => t.ResolvedType.ContainsGenericParameter))
		{
			throw new NotImplementedException();
		}
		if (entry.Types.Count == 1)
		{
			return entry.Types.Single();
		}
		if (entry.Types.Any((ResolvedTypeInfo t) => t.GetRuntimeStorage(_context) == RuntimeStorageKind.ValueType))
		{
			ResolvedTypeInfo type = StackAnalysisUtils.GetWidestValueType(_context, entry.Types);
			if (type != null)
			{
				return type;
			}
			if (entry.Types.All((ResolvedTypeInfo t) => t.IsEnum()))
			{
				type = StackAnalysisUtils.GetWidestValueType(_context, entry.Types.Select((ResolvedTypeInfo t) => t.GetUnderlyingEnumType()));
				if (type != null)
				{
					return type;
				}
			}
			if (entry.Types.Any((ResolvedTypeInfo t) => t.IsSameType(_context.Global.Services.TypeProvider.SystemUIntPtr)))
			{
				return _context.Global.Services.TypeProvider.Resolved.SystemUIntPtr;
			}
			if (entry.Types.Any((ResolvedTypeInfo t) => t.IsSameType(_context.Global.Services.TypeProvider.SystemIntPtr)))
			{
				return _context.Global.Services.TypeProvider.Resolved.SystemIntPtr;
			}
			if (entry.Types.Any((ResolvedTypeInfo t) => t.MetadataType == MetadataType.Var))
			{
				throw new NotImplementedException("Unexpected Entry with Type == MetadataType.Var");
			}
			throw new NotImplementedException();
		}
		if (entry.Types.All((ResolvedTypeInfo t) => t.IsSameType(_context.Global.Services.TypeProvider.SystemIntPtr) || t.IsPointer))
		{
			return _context.Global.Services.TypeProvider.Resolved.SystemIntPtr;
		}
		if (entry.Types.All((ResolvedTypeInfo t) => t.IsSameType(_context.Global.Services.TypeProvider.SystemUIntPtr) || t.IsPointer))
		{
			return _context.Global.Services.TypeProvider.Resolved.SystemUIntPtr;
		}
		if (entry.Types.Select((ResolvedTypeInfo t) => t).Any((ResolvedTypeInfo res) => res?.IsInterface() ?? false))
		{
			return entry.Types.First((ResolvedTypeInfo t) => t.IsInterface());
		}
		if (entry.NullValue)
		{
			ResolvedTypeInfo result = entry.Types.FirstOrDefault((ResolvedTypeInfo t) => t.MetadataType != MetadataType.Object);
			if (result != null)
			{
				return result;
			}
			return entry.Types.First();
		}
		ResolvedTypeInfo resultType = entry.Types.FirstOrDefault((ResolvedTypeInfo t) => t.MetadataType == MetadataType.Object);
		if (resultType != null)
		{
			return resultType;
		}
		return entry.Types.First();
	}
}
