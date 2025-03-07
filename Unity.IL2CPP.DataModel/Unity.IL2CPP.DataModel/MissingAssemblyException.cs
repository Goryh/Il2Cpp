using System;

namespace Unity.IL2CPP.DataModel;

public class MissingAssemblyException : Exception
{
	public MissingAssemblyException(string message)
		: base(message)
	{
	}
}
