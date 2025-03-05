using System.Collections.Generic;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Metadata.RuntimeTypes;

public class Il2CppRuntimeTypeEqualityComparer : IEqualityComparer<IIl2CppRuntimeType>
{
	public static readonly Il2CppRuntimeTypeEqualityComparer Default = new Il2CppRuntimeTypeEqualityComparer();

	private Il2CppRuntimeTypeEqualityComparer()
	{
	}

	public bool Equals(IIl2CppRuntimeType x, IIl2CppRuntimeType y)
	{
		if (x.Attrs == y.Attrs)
		{
			return x.Type == y.Type;
		}
		return false;
	}

	public int GetHashCode(IIl2CppRuntimeType obj)
	{
		return HashCodeHelper.Combine(obj.Type.GetHashCode(), obj.Attrs);
	}
}
