using System.Collections.Generic;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP.StackAnalysis;

public class Entry
{
	private readonly HashSet<ResolvedTypeInfo> _types = new HashSet<ResolvedTypeInfo>(ResolvedTypeEqualityComparer.Instance);

	public ResolvedInstruction Instruction { get; }

	public bool NullValue { get; internal set; }

	public HashSet<ResolvedTypeInfo> Types => _types;

	public Entry()
	{
		Instruction = null;
	}

	public Entry(ResolvedInstruction instruction)
	{
		Instruction = instruction;
	}

	public Entry Clone()
	{
		Entry cloned = new Entry
		{
			NullValue = NullValue
		};
		foreach (ResolvedTypeInfo typeReference in _types)
		{
			cloned.Types.Add(typeReference);
		}
		return cloned;
	}

	public static Entry For(ResolvedInstruction instruction, ResolvedTypeInfo typeReference)
	{
		return new Entry(instruction)
		{
			Types = { typeReference }
		};
	}

	public static Entry ForNull(ResolvedTypeInfo typeReference, ResolvedInstruction instruction)
	{
		return new Entry(instruction)
		{
			NullValue = true,
			Types = { typeReference }
		};
	}
}
