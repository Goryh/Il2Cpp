using System;
using System.Runtime.CompilerServices;

namespace Unity.IL2CPP.CodeWriters;

[InterpolatedStringHandler]
public struct CodeWriterUnindentedInterpolatedStringHandler
{
	private readonly ICodeWriterInterpolatedStringHandlerCallbacks _builder;

	public CodeWriterUnindentedInterpolatedStringHandler(int literalLength, int formattedCount, ICodeWriter builder)
	{
		_builder = (ICodeWriterInterpolatedStringHandlerCallbacks)builder;
	}

	public void AppendLiteral(string value)
	{
		_builder.AppendLiteralUnindented(value);
	}

	public void AppendFormatted<T>(T value)
	{
		_builder.AppendFormattedUnindented(value);
	}

	public void AppendFormatted(ReadOnlySpan<char> value)
	{
		_builder.AppendFormattedUnindented(value);
	}

	public void AppendFormatted(string value)
	{
		_builder.AppendLiteralUnindented(value);
	}
}
