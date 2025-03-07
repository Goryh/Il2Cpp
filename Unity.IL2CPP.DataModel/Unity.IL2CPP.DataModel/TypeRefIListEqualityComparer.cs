using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel;

public class TypeRefIListEqualityComparer : EqualityComparer<IList<TypeReference>>
{
	public override bool Equals(IList<TypeReference> x, IList<TypeReference> y)
	{
		return AreEquals(x, y);
	}

	public static bool AreEquals(IList<TypeReference> x, IList<TypeReference> y)
	{
		if (x.Count != y.Count)
		{
			return false;
		}
		for (int i = 0; i < x.Count; i++)
		{
			if (x[i] != y[i])
			{
				return false;
			}
		}
		return true;
	}

	public override int GetHashCode(IList<TypeReference> obj)
	{
		return GetHashCodeFor(obj);
	}

	public static int GetHashCodeFor(IList<TypeReference> obj)
	{
		int hash = obj.Count;
		for (int i = 0; i < obj.Count; i++)
		{
			hash *= obj[i].GetHashCode();
		}
		return hash;
	}
}
