using System;
using System.Collections.ObjectModel;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

public sealed class PropertyDefinition : MemberReference, IMemberDefinition, ICustomAttributeProvider, IMetadataTokenProvider
{
	internal readonly Mono.Cecil.PropertyDefinition Definition;

	internal bool IsDataModelGenerated { get; }

	public TypeReference PropertyType { get; private set; }

	public ReadOnlyCollection<CustomAttribute> CustomAttributes { get; }

	public PropertyAttributes Attributes { get; }

	public bool HasParameters => Parameters.Count > 0;

	public ReadOnlyCollection<ParameterDefinition> Parameters { get; }

	public MethodDefinition GetMethod { get; }

	public MethodDefinition SetMethod { get; }

	public bool IsSpecialName => Attributes.HasFlag(PropertyAttributes.SpecialName);

	public bool IsRuntimeSpecialName => Attributes.HasFlag(PropertyAttributes.RTSpecialName);

	public new TypeDefinition DeclaringType => (TypeDefinition)base.DeclaringType;

	public override bool IsDefinition => true;

	public override string FullName
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	protected override bool IsFullNameBuilt => false;

	internal PropertyDefinition(TypeDefinition declaringType, ReadOnlyCollection<CustomAttribute> customAttributes, ReadOnlyCollection<ParameterDefinition> parameters, MethodDefinition getMethod, MethodDefinition setMethod, Mono.Cecil.PropertyDefinition definition)
		: this(declaringType, definition.Name, (PropertyAttributes)definition.Attributes, customAttributes, parameters, getMethod, setMethod, MetadataToken.FromCecil(definition), isDataModelGenerated: false)
	{
		Definition = definition;
	}

	internal PropertyDefinition(TypeDefinition declaringType, string name, PropertyAttributes attributes, ReadOnlyCollection<CustomAttribute> customAttributes, ReadOnlyCollection<ParameterDefinition> parameters, MethodDefinition getMethod, MethodDefinition setMethod, MetadataToken metadataToken, bool isDataModelGenerated = true)
		: base(declaringType, metadataToken)
	{
		InitializeName(name);
		CustomAttributes = customAttributes;
		Parameters = parameters;
		GetMethod = getMethod;
		SetMethod = setMethod;
		Attributes = attributes;
		IsDataModelGenerated = isDataModelGenerated;
	}

	public override string ToString()
	{
		return Name;
	}

	internal void InitializePropertyType(TypeReference propertyType)
	{
		PropertyType = propertyType;
	}
}
