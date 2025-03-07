using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.Awesome.Ordering;

public class TypeOrderingComparer : IComparer<TypeDefinition>, IComparer<TypeReference>, IComparer<GenericInstanceType>, IComparer<TypeReference[]>
{
	public int Compare(TypeDefinition x, TypeDefinition y)
	{
		return x.Compare(y);
	}

	public int Compare(TypeReference x, TypeReference y)
	{
		return x.Compare(y);
	}

	public int Compare(GenericInstanceType x, GenericInstanceType y)
	{
		return x.Compare(y);
	}

	public int Compare(TypeReference[] x, TypeReference[] y)
	{
		return x.Compare(y);
	}
}
