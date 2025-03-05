using System.Collections.Generic;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public class MethodUsage
{
	private readonly HashSet<MethodReference> _methods = new HashSet<MethodReference>();

	public void AddMethod(MethodReference method)
	{
		_methods.Add(method);
	}

	public IEnumerable<MethodReference> GetMethods()
	{
		return _methods;
	}
}
