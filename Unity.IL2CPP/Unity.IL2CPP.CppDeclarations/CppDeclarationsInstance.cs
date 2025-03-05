using System;

namespace Unity.IL2CPP.CppDeclarations;

public class CppDeclarationsInstance
{
	public readonly ICppDeclarations Declarations;

	public readonly string Definition;

	public static readonly CppDeclarationsInstance Empty = new CppDeclarationsInstance(new CppDeclarations(), "");

	public CppDeclarationsInstance(ICppDeclarations declarations, string definition)
	{
		if (declarations == null)
		{
			throw new ArgumentNullException("declarations");
		}
		if (definition == null)
		{
			throw new ArgumentNullException("definition");
		}
		Definition = definition;
		Declarations = declarations;
	}

	public override string ToString()
	{
		return Definition;
	}
}
