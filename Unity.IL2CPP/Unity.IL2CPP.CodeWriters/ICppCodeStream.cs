using System;
using Unity.IL2CPP.Contexts.Scheduling.Streams;

namespace Unity.IL2CPP.CodeWriters;

public interface ICppCodeStream : ICodeStream, IDisposable, IStream, ICodeWriter, IDirectWriter, ICppCodeWriter
{
	void Write(ICppCodeStream other);
}
