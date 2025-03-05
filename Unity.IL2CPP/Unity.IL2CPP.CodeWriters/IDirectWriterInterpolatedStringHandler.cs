using System;
using System.Runtime.CompilerServices;

namespace Unity.IL2CPP.CodeWriters;

[InterpolatedStringHandler]
public struct IDirectWriterInterpolatedStringHandler
{
	private readonly IDirectWriterInterpolatedStringHandlerCallbacks _builder;

	public IDirectWriterInterpolatedStringHandler(int literalLength, int formattedCount, IDirectWriter builder)
	{
		_builder = (IDirectWriterInterpolatedStringHandlerCallbacks)builder;
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
