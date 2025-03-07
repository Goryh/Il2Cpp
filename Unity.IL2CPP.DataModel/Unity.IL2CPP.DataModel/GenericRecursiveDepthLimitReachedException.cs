using System;

namespace Unity.IL2CPP.DataModel;

public class GenericRecursiveDepthLimitReachedException : Exception
{
	public GenericRecursiveDepthLimitReachedException()
	{
	}

	public GenericRecursiveDepthLimitReachedException(string message)
		: base(message)
	{
	}
}
