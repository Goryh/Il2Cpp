using System;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP;

internal readonly struct BlockWriter : IDisposable
{
	private readonly ICodeWriter _writer;

	private readonly bool _semicolon;

	public BlockWriter(ICodeWriter writer, bool semicolon = false)
	{
		_writer = writer;
		_semicolon = semicolon;
		writer.BeginBlock();
	}

	public BlockWriter(string startExpression, ICodeWriter writer)
	{
		_writer = writer;
		_semicolon = false;
		writer.WriteLine(startExpression);
		writer.BeginBlock();
	}

	public void Dispose()
	{
		_writer.EndBlock(_semicolon);
	}
}
