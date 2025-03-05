using System;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP;

internal readonly struct IndentWriter : IDisposable
{
	private readonly ICodeWriter _writer;

	public IndentWriter(ICodeWriter writer)
	{
		_writer = writer;
		writer.Indent();
	}

	public IndentWriter(string startExpression, ICodeWriter writer)
	{
		_writer = writer;
		writer.WriteLine(startExpression);
		writer.Indent();
	}

	public void Dispose()
	{
		_writer.Dedent();
	}
}
