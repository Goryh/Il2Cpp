using System;
using System.Text;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.CppDeclarations;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.CodeWriters;

public class GeneratedCodeStringBuilder : CppCodeStringBuilder, IGeneratedCodeWriter, IReadOnlyContextGeneratedCodeWriter, ICppCodeWriter, ICodeWriter, IDirectWriter, IGeneratedCodeBuilder, ICodeBuilder
{
	protected readonly SourceWritingContext _context;

	protected readonly Unity.IL2CPP.CppDeclarations.CppDeclarations _cppDeclarations;

	public new ICppDeclarations Declarations => _cppDeclarations;

	public new SourceWritingContext Context => _context;

	public GeneratedCodeStringBuilder(SourceWritingContext context, StringBuilder builder)
		: this(context, builder, new Unity.IL2CPP.CppDeclarations.CppDeclarations())
	{
	}

	private GeneratedCodeStringBuilder(SourceWritingContext context, StringBuilder builder, Unity.IL2CPP.CppDeclarations.CppDeclarations cppDeclarations)
		: base(context, builder, cppDeclarations)
	{
		_context = context;
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

	void IReadOnlyContextGeneratedCodeWriter.Write(GeneratedCodeString other)
	{
		_cppDeclarations.Add(other.Declarations);
		Write(other.Value);
	}

	void IReadOnlyContextGeneratedCodeWriter.Write(IGeneratedCodeBuilder other)
	{
		((IReadOnlyContextGeneratedCodeWriter)this).Write(other.ToGeneratedCodeStringValue());
	}

	public override string ToCodeStringValue()
	{
		throw new NotSupportedException("Don't use this to avoid loosing track of the Declarations");
	}

	public GeneratedCodeString ToGeneratedCodeStringValue()
	{
		return new GeneratedCodeString(GetStringBuilderValue(), _cppDeclarations);
	}
}
