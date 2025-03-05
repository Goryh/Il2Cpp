using System;
using System.Collections.Generic;

namespace Unity.IL2CPP.Metadata;

public class RgctxEntryNameComparer : IComparer<RgctxEntryName>, IEqualityComparer<RgctxEntryName>
{
	public int Compare(RgctxEntryName x, RgctxEntryName y)
	{
		if (x == y)
		{
			return 0;
		}
		if (y == null)
		{
			return 1;
		}
		if (x == null)
		{
			return -1;
		}
		return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
	}

	public bool Equals(RgctxEntryName x, RgctxEntryName y)
	{
		if (x == y)
		{
			return true;
		}
		if (x == null)
		{
			return false;
		}
		if (y == null)
		{
			return false;
		}
		if (x.GetType() != y.GetType())
		{
			return false;
		}
		return x.Name == y.Name;
	}

	public int GetHashCode(RgctxEntryName obj)
	{
		if (obj.Name == null)
		{
			return 0;
		}
		return obj.Name.GetHashCode();
	}
}
