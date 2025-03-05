using System.IO;
using System.Text;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.CodeWriters;

public class InMemoryCodeWriter : GeneratedCodeWriter
{
	private readonly ChunkedMemoryStream _memoryStream;

	public InMemoryCodeWriter(SourceWritingContext context)
		: this(context, new ChunkedMemoryStream(context))
	{
	}

	private InMemoryCodeWriter(SourceWritingContext context, ChunkedMemoryStream stream)
		: base(context, new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
	{
		_memoryStream = stream;
	}

	public string GetSourceCodeString()
	{
		base.Writer.Flush();
		if (_memoryStream.Length == 0L)
		{
			return string.Empty;
		}
		return Encoding.UTF8.GetString(_memoryStream.GetBuffer(), 0, (int)_memoryStream.Length);
	}
}
