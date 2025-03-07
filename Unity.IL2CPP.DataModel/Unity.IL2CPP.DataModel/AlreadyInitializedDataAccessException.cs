using System;

namespace Unity.IL2CPP.DataModel;

public class AlreadyInitializedDataAccessException : Exception
{
	public AlreadyInitializedDataAccessException(string message)
		: base(message)
	{
	}
}
