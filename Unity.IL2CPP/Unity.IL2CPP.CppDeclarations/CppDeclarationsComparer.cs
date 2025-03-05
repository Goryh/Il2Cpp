using System.Collections.Generic;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel.Awesome.Ordering;

namespace Unity.IL2CPP.CppDeclarations;

public class CppDeclarationsComparer : IComparer<CppDeclarationsData>
{
	private readonly ReadOnlyContext _context;

	public CppDeclarationsComparer(ReadOnlyContext context)
	{
		_context = context;
	}

	public int Compare(CppDeclarationsData x, CppDeclarationsData y)
	{
		return Compare(_context, x, y);
	}

	public static int Compare(ReadOnlyContext context, CppDeclarationsData x, CppDeclarationsData y)
	{
		int xDependencyDepth = x.Type.GetCppDeclarationsDepth(context);
		int yDependencyDepth = y.Type.GetCppDeclarationsDepth(context);
		if (xDependencyDepth > yDependencyDepth)
		{
			return 1;
		}
		if (xDependencyDepth < yDependencyDepth)
		{
			return -1;
		}
		return x.Type.Compare(y.Type);
	}
}
