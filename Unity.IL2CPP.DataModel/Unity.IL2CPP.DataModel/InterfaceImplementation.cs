using System;
using System.Collections.ObjectModel;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

public class InterfaceImplementation : ICustomAttributeProvider, IMetadataTokenProvider
{
	private TypeReference _interfaceType;

	internal readonly Mono.Cecil.InterfaceImplementation Definition;

	public TypeReference InterfaceType
	{
		get
		{
			if (_interfaceType == null)
			{
				throw new ArgumentException("Data has not been initialized yet");
			}
			return _interfaceType;
		}
	}

	public ReadOnlyCollection<CustomAttribute> CustomAttributes { get; }

	public MetadataToken MetadataToken { get; }

	internal InterfaceImplementation(Mono.Cecil.InterfaceImplementation definition, ReadOnlyCollection<CustomAttribute> customAttributes)
	{
		Definition = definition;
		CustomAttributes = customAttributes;
		MetadataToken = MetadataToken.FromCecil(definition);
	}

	internal InterfaceImplementation(ReadOnlyCollection<CustomAttribute> customAttributes, MetadataToken metadataToken)
	{
		Definition = null;
		CustomAttributes = customAttributes;
		MetadataToken = metadataToken;
	}

	internal void InitializeInterfaceType(TypeReference interfaceType)
	{
		_interfaceType = interfaceType;
	}

	public TypeReference ResolveInterfaceImplementation(TypeDefinition declaringType, TypeResolver typeResolver)
	{
		TypeReference resolvedInterface = typeResolver.Resolve(InterfaceType);
		if (!declaringType.IsWindowsRuntime)
		{
			resolvedInterface = declaringType.Context.WindowsRuntimeProjections.ProjectToCLR(resolvedInterface);
		}
		return resolvedInterface;
	}
}
