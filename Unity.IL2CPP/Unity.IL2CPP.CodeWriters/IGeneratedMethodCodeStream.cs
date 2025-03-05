using System;
using NiceIO;
using Unity.IL2CPP.Contexts.Scheduling.Streams;

namespace Unity.IL2CPP.CodeWriters;

public interface IGeneratedMethodCodeStream : IGeneratedCodeStream, IGeneratedCodeWriter, IReadOnlyContextGeneratedCodeWriter, ICppCodeWriter, ICodeWriter, IDirectWriter, IReadOnlyContextGeneratedCodeStream, ICppCodeStream, ICodeStream, IDisposable, IStream, IGeneratedMethodCodeWriter
{
	NPath FileName { get; }

	string GetSourceCodeString();
}
