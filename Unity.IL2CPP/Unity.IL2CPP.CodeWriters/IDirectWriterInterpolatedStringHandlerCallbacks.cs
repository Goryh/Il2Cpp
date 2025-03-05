using System;

namespace Unity.IL2CPP.CodeWriters;

public interface IDirectWriterInterpolatedStringHandlerCallbacks
{
	void AppendFormatted(ReadOnlySpan<char> value);

	void AppendFormatted<T>(T value);

	void AppendLiteral(string value);
}
