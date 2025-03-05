using System.IO;
using System.Text;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.CodeWriters;

public class InMemoryReadOnlyContextCodeWriter : ReadOnlyContextGeneratedCodeWriter
{
	private readonly ChunkedMemoryStream _memoryStream;

	public InMemoryReadOnlyContextCodeWriter(ReadOnlyContext context)
		: this(context, new ChunkedMemoryStream(context))
	{
	}

	private InMemoryReadOnlyContextCodeWriter(ReadOnlyContext context, ChunkedMemoryStream stream)
		: base(context, new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
	{
		_memoryStream = stream;
	}

	public string GetSourceCodeString()
	{
		base.Writer.Flush();
		return Encoding.UTF8.GetString(_memoryStream.GetBuffer(), 0, (int)_memoryStream.Length);
	}
}
