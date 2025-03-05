using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Attributes;

public class CustomAttributeCollectionData
{
	public readonly MethodDefinition Constructor;

	public readonly ReadOnlyCollection<CustomAttributeArgumentData> Arguments;

	public readonly ReadOnlyCollection<CustomAttributeNamedArgumentData> Fields;

	public readonly ReadOnlyCollection<CustomAttributeNamedArgumentData> Properties;

	public TypeDefinition AttributeType => Constructor.DeclaringType;

	public CustomAttributeCollectionData(MethodDefinition constructor, ReadOnlyCollection<CustomAttributeArgumentData> arguments, ReadOnlyCollection<CustomAttributeNamedArgumentData> fields, ReadOnlyCollection<CustomAttributeNamedArgumentData> properties)
	{
		Constructor = constructor;
		Arguments = arguments;
		Fields = fields;
		Properties = properties;
	}
}
