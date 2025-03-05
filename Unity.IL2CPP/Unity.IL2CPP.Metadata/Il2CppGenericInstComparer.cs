using System.Collections.Generic;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Metadata;

internal class Il2CppGenericInstComparer : EqualityComparer<IList<TypeReference>>
{
	public override bool Equals(IList<TypeReference> x, IList<TypeReference> y)
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
		int hashCode = obj.Count;
		for (int i = 0; i < obj.Count; i++)
		{
			hashCode = hashCode * 486187739 + obj[i].GetHashCode();
		}
		return hashCode;
	}
}
