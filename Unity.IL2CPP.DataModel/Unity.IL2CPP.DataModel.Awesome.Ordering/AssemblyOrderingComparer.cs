using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.Awesome.Ordering;

public class AssemblyOrderingComparer : IComparer<AssemblyDefinition>
{
	public int Compare(AssemblyDefinition x, AssemblyDefinition y)
	{
		return x.Compare(y);
	}
}
