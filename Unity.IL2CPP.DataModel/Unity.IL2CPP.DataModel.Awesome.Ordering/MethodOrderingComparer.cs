using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.Awesome.Ordering;

public class MethodOrderingComparer : IComparer<GenericInstanceMethod>, IComparer<MethodReference>, IComparer<MethodDefinition>
{
	public int Compare(GenericInstanceMethod x, GenericInstanceMethod y)
	{
		return x.Compare(y);
	}

	public int Compare(MethodReference x, MethodReference y)
	{
		return x.Compare(y);
	}

	public int Compare(MethodDefinition x, MethodDefinition y)
	{
		return x.Compare(y);
	}
}
