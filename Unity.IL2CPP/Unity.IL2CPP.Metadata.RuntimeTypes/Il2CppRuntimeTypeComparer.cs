using System.Collections.Generic;
using Unity.IL2CPP.DataModel.Awesome.Ordering;

namespace Unity.IL2CPP.Metadata.RuntimeTypes;

public class Il2CppRuntimeTypeComparer : IComparer<IIl2CppRuntimeType>
{
	public int Compare(IIl2CppRuntimeType x, IIl2CppRuntimeType y)
	{
		return DoCompare(x, y);
	}

	public static int DoCompare(IIl2CppRuntimeType x, IIl2CppRuntimeType y)
	{
		int result = x.Type.Compare(y.Type);
		if (result != 0)
		{
			return result;
		}
		result = x.Attrs - y.Attrs;
		if (result != 0)
		{
			return result;
		}
		return 0;
	}
}
