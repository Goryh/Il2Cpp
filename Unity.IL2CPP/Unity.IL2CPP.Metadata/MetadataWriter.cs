using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP.Metadata;

public class MetadataWriter<TWriter> where TWriter : ICppCodeWriter
{
	public enum ArrayTerminator
	{
		None,
		Null
	}

	private readonly TWriter _writer;

	protected TWriter Writer => _writer;

	protected MetadataWriter(TWriter writer)
	{
		_writer = writer;
	}

	protected void WriteLine(string line)
	{
		_writer.WriteLine(line);
	}

	protected void Write(string format)
	{
		_writer.Write(format);
	}

	protected void BeginBlock()
	{
		_writer.BeginBlock();
	}

	protected void EndBlock(bool semicolon)
	{
		_writer.EndBlock(semicolon);
	}

	protected void WriteArrayInitializer(IEnumerable<string> initializers, ArrayTerminator terminator = ArrayTerminator.None)
	{
		BeginBlock();
		foreach (string initializer in initializers)
		{
			WriteLine(initializer + ",");
		}
		if (terminator == ArrayTerminator.Null)
		{
			WriteLine("NULL");
		}
		EndBlock(semicolon: true);
	}
}
