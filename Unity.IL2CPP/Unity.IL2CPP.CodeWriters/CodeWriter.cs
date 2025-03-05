using System;
using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;

namespace Unity.IL2CPP.CodeWriters;

public class CodeWriter : BaseCodeWriter, ICodeStream, IDisposable, IStream, ICodeWriter, IDirectWriter
{
	private readonly bool _owns;

	public StreamWriter Writer { get; private set; }

	public long Length => Writer.BaseStream.Length;

	public override bool Empty => Writer.BaseStream.Length == 0;

	public CodeWriter(ReadOnlyContext context, StreamWriter writer, bool owns = true)
		: base(context)
	{
		_owns = owns;
		Writer = writer;
	}

	public virtual void Dispose()
	{
		if (_owns)
		{
			Writer.Dispose();
		}
	}

	protected override void AppendFormattedUnindented<T>(T value)
	{
		WriteInternal(value.ToString());
	}

	protected override void AppendFormatted<T>(T value)
	{
		AppendLiteral(value.ToString());
	}

	protected override void DirectAppendFormatted<T>(T value)
	{
		WriteInternal(value.ToString());
	}

	protected override void WriteInternal(ReadOnlySpan<char> s)
	{
		Writer.Write(s);
	}

	protected override void WriteInternal(char s)
	{
		Writer.Write(s);
	}

	public override void Flush()
	{
		Writer.Flush();
	}

	void ICodeStream.Write(ICodeStream other)
	{
		Writer.Flush();
		other.Writer.Flush();
		Stream baseStream = other.Writer.BaseStream;
		long originalPosition = baseStream.Position;
		baseStream.Seek(0L, SeekOrigin.Begin);
		baseStream.CopyTo(Writer.BaseStream);
		baseStream.Seek(originalPosition, SeekOrigin.Begin);
		Writer.Flush();
	}
}
