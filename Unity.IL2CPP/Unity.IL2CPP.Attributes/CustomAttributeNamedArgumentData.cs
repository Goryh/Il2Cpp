using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Attributes;

public readonly struct CustomAttributeNamedArgumentData
{
	public readonly CustomAttributeArgumentData Argument;

	public readonly IMemberDefinition FieldOrProperty;

	private CustomAttributeNamedArgumentData(CustomAttributeArgumentData argument, IMemberDefinition fieldOrProperty)
	{
		Argument = argument;
		FieldOrProperty = fieldOrProperty;
	}

	public static CustomAttributeNamedArgumentData Create(GlobalPrimaryCollectionContext context, CustomAttributeNamedArgument arg, in AttributeDataItem data, IMemberDefinition fieldOrProperty)
	{
		return new CustomAttributeNamedArgumentData(CustomAttributeArgumentData.Create(context, arg.Argument, in data), fieldOrProperty);
	}
}
