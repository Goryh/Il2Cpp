using System;
using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.CppDeclarations;

namespace Unity.IL2CPP.CodeWriters;

public abstract class GeneratedCodeWriter : ReadOnlyContextGeneratedCodeWriter, IGeneratedCodeStream, IGeneratedCodeWriter, IReadOnlyContextGeneratedCodeWriter, ICppCodeWriter, ICodeWriter, IDirectWriter, IReadOnlyContextGeneratedCodeStream, ICppCodeStream, ICodeStream, IDisposable, IStream
{
	protected readonly SourceWritingContext _context;

	public new SourceWritingContext Context => _context;

	protected GeneratedCodeWriter(SourceWritingContext context, StreamWriter stream)
		: this(context, stream, new Unity.IL2CPP.CppDeclarations.CppDeclarations())
	{
	}

	private GeneratedCodeWriter(SourceWritingContext context, StreamWriter stream, Unity.IL2CPP.CppDeclarations.CppDeclarations cppDeclarations)
		: base(context, stream, cppDeclarations)
	{
		_context = context;
	}
}
