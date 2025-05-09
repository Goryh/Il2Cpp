using System;
using System.IO;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.CppDeclarations;

namespace Unity.IL2CPP.CodeWriters;

public abstract class CppCodeWriter : CodeWriter, ICppCodeStream, ICodeStream, IDisposable, IStream, ICodeWriter, IDirectWriter, ICppCodeWriter
{
	private readonly CppDeclarationsBasic _cppDeclarations;

	protected CppDeclarationsBasic Declarations => _cppDeclarations;

	protected CppCodeWriter(ReadOnlyContext context, StreamWriter writer, CppDeclarationsBasic cppDeclarations)
		: base(context, writer)
	{
		_cppDeclarations = cppDeclarations;
	}

	protected CppCodeWriter(ReadOnlyContext context, StreamWriter writer)
		: this(context, writer, new CppDeclarationsBasic())
	{
	}

	public void AddInclude(string path)
	{
		_cppDeclarations._includes.Add(path.InQuotes());
	}

	public void AddStdInclude(string path)
	{
		_cppDeclarations._includes.Add("<" + path + ">");
	}

	public void AddForwardDeclaration(string declaration)
	{
		if (string.IsNullOrEmpty(declaration))
		{
			throw new ArgumentException("Type forward declaration must not be empty.", "declaration");
		}
		_cppDeclarations._rawTypeForwardDeclarations.Add(declaration);
	}

	public void AddMethodForwardDeclaration(string declaration)
	{
		if (string.IsNullOrEmpty(declaration))
		{
			throw new ArgumentException("Method forward declaration must not be empty.", "declaration");
		}
		_cppDeclarations._rawMethodForwardDeclarations.Add(declaration);
	}

	void ICppCodeStream.Write(ICppCodeStream other)
	{
		base.Writer.Flush();
		other.Writer.Flush();
		Stream baseStream = other.Writer.BaseStream;
		long originalPosition = baseStream.Position;
		baseStream.Seek(0L, SeekOrigin.Begin);
		baseStream.CopyTo(base.Writer.BaseStream);
		baseStream.Seek(originalPosition, SeekOrigin.Begin);
		base.Writer.Flush();
	}
}
