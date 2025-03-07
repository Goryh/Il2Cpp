using System;

namespace Unity.IL2CPP.DataModel;

public class UninitializedDataAccessException : Exception
{
	public UninitializedDataAccessException(string message)
		: base(message)
	{
	}
}
