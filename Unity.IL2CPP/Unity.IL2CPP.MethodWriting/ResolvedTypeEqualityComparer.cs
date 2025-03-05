using System.Collections.Generic;

namespace Unity.IL2CPP.MethodWriting;

public class ResolvedTypeEqualityComparer : EqualityComparer<ResolvedTypeInfo>
{
	public static readonly ResolvedTypeEqualityComparer Instance = new ResolvedTypeEqualityComparer();

	private ResolvedTypeEqualityComparer()
	{
	}

	public override bool Equals(ResolvedTypeInfo x, ResolvedTypeInfo y)
	{
		return AreEqual(x, y);
	}

	public static bool AreEqual(ResolvedTypeInfo x, ResolvedTypeInfo y)
	{
		return x.IsSameType(y);
	}

	public override int GetHashCode(ResolvedTypeInfo obj)
	{
		return obj.GetHashCode();
	}
}
