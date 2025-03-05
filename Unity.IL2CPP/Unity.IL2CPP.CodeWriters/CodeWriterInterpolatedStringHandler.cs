using System;
using System.Runtime.CompilerServices;

namespace Unity.IL2CPP.CodeWriters;

[InterpolatedStringHandler]
public struct CodeWriterInterpolatedStringHandler
{
	private readonly ICodeWriterInterpolatedStringHandlerCallbacks _builder;

	public CodeWriterInterpolatedStringHandler(int literalLength, int formattedCount, ICodeWriter builder)
	{
		_builder = (ICodeWriterInterpolatedStringHandlerCallbacks)builder;
	}

	public void AppendLiteral(string value)
	{
		_builder.AppendLiteral(value);
	}

	public void AppendFormatted<T>(T value)
	{
		_builder.AppendFormatted(value);
	}

	public void AppendFormatted(ReadOnlySpan<char> value)
	{
		_builder.AppendFormatted(value);
	}

	public void AppendFormatted(string value)
	{
		_builder.AppendFormatted(value);
	}
}
