using System;
using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.CppDeclarations;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.CodeWriters;

public class ReadOnlyContextGeneratedCodeWriter : CppCodeWriter, IReadOnlyContextGeneratedCodeStream, IReadOnlyContextGeneratedCodeWriter, ICppCodeWriter, ICodeWriter, IDirectWriter, ICppCodeStream, ICodeStream, IDisposable, IStream
{
	protected readonly Unity.IL2CPP.CppDeclarations.CppDeclarations _cppDeclarations;

	public new ICppDeclarations Declarations => _cppDeclarations;

	protected ReadOnlyContextGeneratedCodeWriter(ReadOnlyContext context, StreamWriter stream)
		: this(context, stream, new Unity.IL2CPP.CppDeclarations.CppDeclarations())
	{
	}

	protected ReadOnlyContextGeneratedCodeWriter(ReadOnlyContext context, StreamWriter stream, Unity.IL2CPP.CppDeclarations.CppDeclarations cppDeclarations)
		: base(context, stream, cppDeclarations)
	{
		_cppDeclarations = cppDeclarations;
	}

	public void AddInclude(TypeReference type)
	{
		_cppDeclarations._typeIncludes.Add(type);
	}

	public void AddForwardDeclaration(TypeReference typeReference)
	{
		if (typeReference == null)
		{
			throw new ArgumentNullException("typeReference");
		}
		_cppDeclarations._forwardDeclarations.Add(GeneratedCodeWriterExtensions.GetForwardDeclarationType(typeReference));
	}

	public void WriteExternForIl2CppType(IIl2CppRuntimeType type)
	{
		_cppDeclarations._typeExterns.Add(type);
	}

	public void WriteExternForIl2CppGenericInst(IIl2CppRuntimeType[] type)
	{
		_cppDeclarations._genericInstExterns.Add(type);
	}

	public void WriteExternForGenericClass(TypeReference type)
	{
		_cppDeclarations._genericClassExterns.Add(type);
	}

	public void WriteExternForArray(ArrayType type)
	{
		_cppDeclarations._arrayTypes.Add(type);
	}

	void IReadOnlyContextGeneratedCodeStream.Write(IReadOnlyContextGeneratedCodeStream other)
	{
		_cppDeclarations.Add(other.Declarations);
		base.Writer.Flush();
		other.Writer.Flush();
		Stream baseStream = other.Writer.BaseStream;
		long originalPosition = baseStream.Position;
		baseStream.Seek(0L, SeekOrigin.Begin);
		baseStream.CopyTo(base.Writer.BaseStream);
		baseStream.Seek(originalPosition, SeekOrigin.Begin);
		base.Writer.Flush();
	}

	void IReadOnlyContextGeneratedCodeWriter.Write(GeneratedCodeString other)
	{
		_cppDeclarations.Add(other.Declarations);
		Write(other.Value);
	}

	void IReadOnlyContextGeneratedCodeWriter.Write(IGeneratedCodeBuilder other)
	{
		((IReadOnlyContextGeneratedCodeWriter)this).Write(other.ToGeneratedCodeStringValue());
	}
}
