namespace Unity.IL2CPP.DataModel.Modify.Builders;

public class MethodDefinitionBuilderOnTypeBuilder : MethodDefinitionBuilder
{
	private readonly TypeDefinitionBuilder _parent;

	internal MethodDefinitionBuilderOnTypeBuilder(EditContext context, TypeDefinitionBuilder parent, string name, MethodAttributes attributes, TypeReference returnType)
		: base(context, name, attributes, returnType)
	{
		_parent = parent;
	}

	public TypeDefinitionBuilder DeclaringType()
	{
		return _parent;
	}
}
