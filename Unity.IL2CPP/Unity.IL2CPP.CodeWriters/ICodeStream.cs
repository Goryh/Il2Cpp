using System;
using System.IO;
using Unity.IL2CPP.Contexts.Scheduling.Streams;

namespace Unity.IL2CPP.CodeWriters;

public interface ICodeStream : IDisposable, IStream, ICodeWriter, IDirectWriter
{
	StreamWriter Writer { get; }

	void Write(ICodeStream other);
}
