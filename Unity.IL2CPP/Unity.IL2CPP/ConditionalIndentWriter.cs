using System;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP;

internal readonly struct ConditionalIndentWriter : IDisposable
{
	private readonly bool _condition;

	private readonly ICodeWriter _writer;

	public ConditionalIndentWriter(bool condition, string startExpression, ICodeWriter writer)
	{
		_condition = condition;
		_writer = writer;
		if (condition)
		{
			writer.WriteLine(startExpression);
			writer.Indent();
		}
	}

	public void Dispose()
	{
		if (_condition)
		{
			_writer.Dedent();
		}
	}
}
