using System.Collections.Generic;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

internal class TypeReferenceArrayEqualityComparer : EqualityComparer<TypeReference[]>
{
	public override bool Equals(TypeReference[] x, TypeReference[] y)
	{
		if (x.Length != y.Length)
		{
			return false;
		}
		for (int i = 0; i < x.Length; i++)
		{
			if (x[i] != y[i])
			{
				return false;
			}
		}
		return true;
	}

	public override int GetHashCode(TypeReference[] obj)
	{
		int hashcode = 31 * obj.Length;
		for (int i = 0; i < obj.Length; i++)
		{
			hashcode += 7 * obj[i].GetHashCode();
		}
		return hashcode;
	}
}
