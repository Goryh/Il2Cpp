using System;
using System.Text;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.CppDeclarations;

namespace Unity.IL2CPP.CodeWriters;

public class CppCodeStringBuilder : CodeStringBuilder, ICppCodeWriter, ICodeWriter, IDirectWriter
{
	private readonly CppDeclarationsBasic _cppDeclarations;

	protected CppDeclarationsBasic Declarations => _cppDeclarations;

	protected CppCodeStringBuilder(ReadOnlyContext context, StringBuilder builder, CppDeclarationsBasic cppDeclarations)
		: base(context, builder)
	{
		_cppDeclarations = cppDeclarations;
	}

	public CppCodeStringBuilder(ReadOnlyContext context, StringBuilder builder)
		: this(context, builder, new CppDeclarationsBasic())
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
}
