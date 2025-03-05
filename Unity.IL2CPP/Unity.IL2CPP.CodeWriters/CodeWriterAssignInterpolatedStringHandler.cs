using System;
using System.Runtime.CompilerServices;

namespace Unity.IL2CPP.CodeWriters;

[InterpolatedStringHandler]
public struct CodeWriterAssignInterpolatedStringHandler
{
	private readonly int _literalLength;

	private readonly int _formattedCount;

	private int _currentLiteralLength;

	private int _currentFormattedCount;

	private readonly ICodeWriterInterpolatedStringHandlerCallbacks _builder;

	public CodeWriterAssignInterpolatedStringHandler(int literalLength, int formattedCount, ICodeWriter builder)
	{
		_literalLength = literalLength;
		_formattedCount = formattedCount;
		_currentLiteralLength = 0;
		_currentFormattedCount = 0;
		_builder = (ICodeWriterInterpolatedStringHandlerCallbacks)builder;
	}

	public void AppendLiteral(string value)
	{
		_builder.AppendLiteral(value);
		_currentLiteralLength += value.Length;
		CheckAndWriteAssignment();
	}

	public void AppendFormatted<T>(T value)
	{
		_builder.AppendFormatted(value);
		_currentFormattedCount++;
		CheckAndWriteAssignment();
	}

	public void AppendFormatted(ReadOnlySpan<char> value)
	{
		_builder.AppendFormatted(value);
		_currentFormattedCount++;
		CheckAndWriteAssignment();
	}

	public void AppendFormatted(string value)
	{
		_builder.AppendFormatted(value);
		_currentFormattedCount++;
		CheckAndWriteAssignment();
	}

	private void CheckAndWriteAssignment()
	{
		if (_currentLiteralLength >= _literalLength && _currentFormattedCount >= _formattedCount)
		{
			_builder.AppendLiteral(" = ");
		}
	}
}
