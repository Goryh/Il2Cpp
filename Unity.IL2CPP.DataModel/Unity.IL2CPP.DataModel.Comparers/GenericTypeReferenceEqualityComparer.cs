using System.Collections.Generic;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic;

namespace Unity.IL2CPP.DataModel.Comparers;

public class GenericTypeReferenceEqualityComparer : EqualityComparer<GenericTypeReference>
{
	public override bool Equals(GenericTypeReference x, GenericTypeReference y)
	{
		if (x.GenericInstance == y.GenericInstance)
		{
			return x.TypeReference == y.TypeReference;
		}
		return false;
	}

	public override int GetHashCode(GenericTypeReference obj)
	{
		return HashCodeHelper.Combine(obj.TypeReference.GetHashCode(), obj.GenericInstance.GetHashCode());
	}
}
