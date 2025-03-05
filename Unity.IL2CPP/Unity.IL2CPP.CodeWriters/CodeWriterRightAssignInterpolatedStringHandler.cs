using System;
using System.Runtime.CompilerServices;

namespace Unity.IL2CPP.CodeWriters;

[InterpolatedStringHandler]
public readonly struct CodeWriterRightAssignInterpolatedStringHandler
{
	private readonly ICodeWriterInterpolatedStringHandlerCallbacks _builder;

	public CodeWriterRightAssignInterpolatedStringHandler(int literalLength, int formattedCount, ICodeWriter builder, string left)
	{
		_builder = (ICodeWriterInterpolatedStringHandlerCallbacks)builder;
		_builder.AppendLiteral(left);
		_builder.AppendLiteral(" = ");
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
