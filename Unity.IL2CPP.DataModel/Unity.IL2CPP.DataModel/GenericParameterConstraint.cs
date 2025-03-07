using System;
using System.Collections.ObjectModel;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

public class GenericParameterConstraint : ICustomAttributeProvider, IMetadataTokenProvider
{
	internal readonly Mono.Cecil.GenericParameterConstraint Definition;

	private TypeReference _constraintType;

	public ReadOnlyCollection<CustomAttribute> CustomAttributes { get; }

	public TypeReference ConstraintType
	{
		get
		{
			if (_constraintType == null)
			{
				throw new ArgumentException("Data has not been initialized yet");
			}
			return _constraintType;
		}
	}

	public MetadataToken MetadataToken { get; }

	internal GenericParameterConstraint(ReadOnlyCollection<CustomAttribute> customAttributes, Mono.Cecil.GenericParameterConstraint definition)
	{
		Definition = definition;
		CustomAttributes = customAttributes;
		MetadataToken = MetadataToken.FromCecil(definition);
	}

	internal void InitializeConstraintType(TypeReference constraintType)
	{
		_constraintType = constraintType;
	}
}
