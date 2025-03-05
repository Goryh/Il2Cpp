using System.Collections.Generic;

namespace Unity.IL2CPP.Metadata;

public class Il2CppMethodSpecEqualityComparer : EqualityComparer<Il2CppMethodSpec>
{
	public override bool Equals(Il2CppMethodSpec x, Il2CppMethodSpec y)
	{
		return x.GenericMethod == y.GenericMethod;
	}

	public override int GetHashCode(Il2CppMethodSpec obj)
	{
		return obj.GenericMethod.GetHashCode();
	}
}
