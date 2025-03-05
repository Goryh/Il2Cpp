using System;
using Unity.IL2CPP.Contexts.Scheduling.Streams;

namespace Unity.IL2CPP.CodeWriters;

public interface IGeneratedCodeStream : IGeneratedCodeWriter, IReadOnlyContextGeneratedCodeWriter, ICppCodeWriter, ICodeWriter, IDirectWriter, IReadOnlyContextGeneratedCodeStream, ICppCodeStream, ICodeStream, IDisposable, IStream
{
}
