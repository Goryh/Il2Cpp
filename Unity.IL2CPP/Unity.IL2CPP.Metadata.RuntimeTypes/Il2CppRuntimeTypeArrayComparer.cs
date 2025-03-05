using System;
using System.Collections.Generic;

namespace Unity.IL2CPP.Metadata.RuntimeTypes;

internal class Il2CppRuntimeTypeArrayComparer : IComparer<IIl2CppRuntimeType[]>
{
	public int Compare(IIl2CppRuntimeType[] x, IIl2CppRuntimeType[] y)
	{
		return DoCompare(x, y);
	}

	public static int DoCompare(IIl2CppRuntimeType[] x, IIl2CppRuntimeType[] y)
	{
		int min = Math.Min(x.Length, y.Length);
		for (int i = 0; i < min; i++)
		{
			int elementCompare = Il2CppRuntimeTypeComparer.DoCompare(x[i], y[i]);
			if (elementCompare != 0)
			{
				return elementCompare;
			}
		}
		return x.Length - y.Length;
	}
}
