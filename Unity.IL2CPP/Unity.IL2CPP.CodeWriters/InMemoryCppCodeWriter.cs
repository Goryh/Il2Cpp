using System.IO;
using System.Text;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.CodeWriters;

public class InMemoryCppCodeWriter : CppCodeWriter
{
	public InMemoryCppCodeWriter(ReadOnlyContext context)
		: this(context, new ChunkedMemoryStream(context))
	{
	}

	private InMemoryCppCodeWriter(ReadOnlyContext context, ChunkedMemoryStream stream)
		: base(context, new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
	{
	}
}
