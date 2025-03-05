using System.Collections.Generic;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP;

public class IndirectCallSignatureComparer : Comparer<IndirectCallSignature>
{
	public override int Compare(IndirectCallSignature x, IndirectCallSignature y)
	{
		int signaturesEqual = Il2CppRuntimeTypeArrayComparer.DoCompare(x.Signature, y.Signature);
		if (signaturesEqual != 0)
		{
			return signaturesEqual;
		}
		return x.Usage.CompareTo(y.Usage);
	}
}
