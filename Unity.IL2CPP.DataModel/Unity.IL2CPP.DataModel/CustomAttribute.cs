using System;
using System.Collections.ObjectModel;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

public class CustomAttribute
{
	internal readonly Mono.Cecil.CustomAttribute Definition;

	private MethodDefinition _constructor;

	public readonly ReadOnlyCollection<CustomAttributeArgument> ConstructorArguments;

	public readonly ReadOnlyCollection<CustomAttributeNamedArgument> Fields;

	public readonly ReadOnlyCollection<CustomAttributeNamedArgument> Properties;

	public MethodDefinition Constructor
	{
		get
		{
			if (_constructor == null)
			{
				throw new ArgumentException("Data has not been initialized yet");
			}
			return _constructor;
		}
	}

	public TypeReference AttributeType => Constructor.DeclaringType;

	public bool HasFields => Fields.Count > 0;

	public bool HasProperties => Properties.Count > 0;

	public bool HasConstructorArguments => ConstructorArguments.Count > 0;

	public CustomAttribute(Mono.Cecil.CustomAttribute definition, ReadOnlyCollection<CustomAttributeArgument> constructorArguments, ReadOnlyCollection<CustomAttributeNamedArgument> fields, ReadOnlyCollection<CustomAttributeNamedArgument> properties)
	{
		Definition = definition;
		ConstructorArguments = constructorArguments;
		Fields = fields;
		Properties = properties;
	}

	internal void InitializeConstructor(MethodDefinition constructor)
	{
		_constructor = constructor;
	}
}
