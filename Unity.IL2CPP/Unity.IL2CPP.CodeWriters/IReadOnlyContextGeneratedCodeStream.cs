using System;
using Unity.IL2CPP.Contexts.Scheduling.Streams;

namespace Unity.IL2CPP.CodeWriters;

public interface IReadOnlyContextGeneratedCodeStream : IReadOnlyContextGeneratedCodeWriter, ICppCodeWriter, ICodeWriter, IDirectWriter, ICppCodeStream, ICodeStream, IDisposable, IStream
{
	void Write(IReadOnlyContextGeneratedCodeStream other);
}
