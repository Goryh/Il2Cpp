using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil.Cil;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel;

[DebuggerDisplay("{ToString()} ({VariableType})")]
public class VariableDefinition
{
	private static readonly ReadOnlyCollection<string> VariableNameCache;

	public TypeReference VariableType { get; }

	public bool IsPinned { get; }

	public int Index { get; }

	public string DebugName { get; }

	public string CppName { get; }

	public bool HasDebugName => !string.IsNullOrEmpty(DebugName);

	static VariableDefinition()
	{
		VariableNameCache = (from i in Enumerable.Range(0, 100)
			select $"V_{i}").ToArray().AsReadOnly();
	}

	internal VariableDefinition(TypeReference variableType, Mono.Cecil.Cil.VariableDefinition definition, string debugName)
		: this(variableType, definition.IsPinned, definition.Index, debugName)
	{
		VariableType = variableType;
		IsPinned = definition.IsPinned;
		Index = definition.Index;
		DebugName = debugName;
	}

	internal VariableDefinition(TypeReference variableType, bool isPinned, int index, string debugName)
	{
		VariableType = variableType;
		IsPinned = isPinned;
		Index = index;
		DebugName = debugName;
		CppName = ((index < VariableNameCache.Count) ? VariableNameCache[index] : $"V_{index}");
	}

	public override string ToString()
	{
		if (!HasDebugName)
		{
			return CppName;
		}
		return DebugName;
	}
}
