using System;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP;

public readonly struct ConditionalBlockWriter : IDisposable
{
	private readonly ICodeWriter _writer;

	private readonly bool _condition;

	public ConditionalBlockWriter(bool condition, string startExpression, ICodeWriter writer)
	{
		_writer = writer;
		_condition = condition;
		if (condition)
		{
			writer.WriteLine(startExpression);
			writer.BeginBlock();
		}
	}

	public void Dispose()
	{
		if (_condition)
		{
			_writer.EndBlock();
		}
	}
}
