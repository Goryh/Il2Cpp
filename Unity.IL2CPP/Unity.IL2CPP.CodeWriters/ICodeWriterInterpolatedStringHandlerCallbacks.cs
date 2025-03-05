using System;

namespace Unity.IL2CPP.CodeWriters;

public interface ICodeWriterInterpolatedStringHandlerCallbacks
{
	void AppendFormatted(ReadOnlySpan<char> value);

	void AppendFormatted<T>(T value);

	void AppendLiteral(string value);

	void AppendFormattedUnindented(ReadOnlySpan<char> value);

	void AppendFormattedUnindented<T>(T value);

	void AppendLiteralUnindented(string value);
}
