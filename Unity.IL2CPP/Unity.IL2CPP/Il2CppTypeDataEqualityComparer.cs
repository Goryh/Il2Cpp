using System.Collections.Generic;

namespace Unity.IL2CPP;

internal class Il2CppTypeDataEqualityComparer : EqualityComparer<Il2CppTypeData>
{
	public override bool Equals(Il2CppTypeData x, Il2CppTypeData y)
	{
		if (x.Attrs == y.Attrs)
		{
			return x.Type == y.Type;
		}
		return false;
	}

	public override int GetHashCode(Il2CppTypeData obj)
	{
		return obj.Type.GetHashCode() + obj.Attrs;
	}
}
