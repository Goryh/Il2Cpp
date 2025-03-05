using System.Collections.Generic;

namespace Unity.IL2CPP.Metadata.RuntimeTypes;

internal class Il2CppRuntimeTypeArrayEqualityComparer : IEqualityComparer<IIl2CppRuntimeType[]>
{
	public static readonly Il2CppRuntimeTypeArrayEqualityComparer Default = new Il2CppRuntimeTypeArrayEqualityComparer();

	private Il2CppRuntimeTypeArrayEqualityComparer()
	{
	}

	public bool Equals(IIl2CppRuntimeType[] x, IIl2CppRuntimeType[] y)
	{
		return AreEqual(x, y);
	}

	public static bool AreEqual(IIl2CppRuntimeType[] x, IIl2CppRuntimeType[] y)
	{
		if (x.Length != y.Length)
		{
			return false;
		}
		for (int i = 0; i < x.Length; i++)
		{
			if (!Il2CppRuntimeTypeEqualityComparer.Default.Equals(x[i], y[i]))
			{
				return false;
			}
		}
		return true;
	}

	public int GetHashCode(IIl2CppRuntimeType[] obj)
	{
		return HashCodeFor(obj);
	}

	public static int HashCodeFor(IIl2CppRuntimeType[] obj)
	{
		int hashcode = 31 * obj.Length;
		for (int i = 0; i < obj.Length; i++)
		{
			hashcode += 7 * Il2CppRuntimeTypeEqualityComparer.Default.GetHashCode(obj[i]);
		}
		return hashcode;
	}
}
