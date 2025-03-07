using System;

namespace Unity.IL2CPP.DataModel;

public class WinmdResolutionException : Exception
{
	public WinmdResolutionException(string message)
		: base(message)
	{
	}
}
