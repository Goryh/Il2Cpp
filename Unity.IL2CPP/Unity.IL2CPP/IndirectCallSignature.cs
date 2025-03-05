using System;
using System.Linq;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP;

public readonly struct IndirectCallSignature : IEquatable<IndirectCallSignature>
{
	public readonly IIl2CppRuntimeType[] Signature;

	public readonly IndirectCallUsage Usage;

	public IndirectCallSignature(IIl2CppRuntimeType[] signature, IndirectCallUsage usage)
	{
		Signature = signature;
		Usage = usage;
	}

	public override bool Equals(object obj)
	{
		if (obj is IndirectCallSignature other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Il2CppRuntimeTypeArrayEqualityComparer.HashCodeFor(Signature), (int)Usage);
	}

	public bool Equals(IndirectCallSignature other)
	{
		if (Il2CppRuntimeTypeArrayEqualityComparer.AreEqual(Signature, other.Signature))
		{
			return Usage == other.Usage;
		}
		return false;
	}

	public override string ToString()
	{
		return $"{Signature[0].Type}({(from s in Signature.Skip(1)
			select s.Type.ToString()).AggregateWithComma()}) [{Usage}]";
	}
}
