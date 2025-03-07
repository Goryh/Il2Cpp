namespace Unity.IL2CPP.DataModel.Modify.Builders;

public class FieldDefinitionBuilderOnTypeBuilder : FieldDefinitionBuilder
{
	private readonly TypeDefinitionBuilder _parent;

	internal FieldDefinitionBuilderOnTypeBuilder(EditContext context, TypeDefinitionBuilder parent, string fieldName, FieldAttributes attributes, TypeReference fieldType)
		: base(context, fieldName, attributes, fieldType)
	{
		_parent = parent;
	}

	public TypeDefinitionBuilder DeclaringType()
	{
		return _parent;
	}
}
