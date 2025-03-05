using System;

namespace il2cpp.Compilation;

public class BuilderFailedException : Exception
{
	public BuilderFailedException(string failureReason)
		: base(failureReason)
	{
	}

	public BuilderFailedException(string failureReason, Exception innerException)
		: base(failureReason, innerException)
	{
	}
}
