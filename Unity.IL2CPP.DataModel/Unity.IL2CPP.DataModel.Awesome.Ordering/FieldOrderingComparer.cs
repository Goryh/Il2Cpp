using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.Awesome.Ordering;

public class FieldOrderingComparer : IComparer<FieldReference>
{
	public int Compare(FieldReference x, FieldReference y)
	{
		return x.Compare(y);
	}
}
