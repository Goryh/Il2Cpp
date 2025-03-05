using System.Collections.Generic;

namespace Unity.IL2CPP;

internal class Il2CppRuntimeFieldReferenceEqualityComparer : IEqualityComparer<Il2CppRuntimeFieldReference>
{
	public static readonly Il2CppRuntimeFieldReferenceEqualityComparer Default = new Il2CppRuntimeFieldReferenceEqualityComparer();

	private Il2CppRuntimeFieldReferenceEqualityComparer()
	{
	}

	public bool Equals(Il2CppRuntimeFieldReference x, Il2CppRuntimeFieldReference y)
	{
		return x.Field == y.Field;
	}

	public int GetHashCode(Il2CppRuntimeFieldReference obj)
	{
		return obj.Field.GetHashCode();
	}
}
