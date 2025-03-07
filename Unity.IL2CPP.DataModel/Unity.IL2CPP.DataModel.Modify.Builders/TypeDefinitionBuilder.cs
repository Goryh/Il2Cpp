using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.BuildLogic.Inflation;
using Unity.IL2CPP.DataModel.BuildLogic.Populaters;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Modify.Definitions;
using Unity.IL2CPP.DataModel.RuntimeStorage;

namespace Unity.IL2CPP.DataModel.Modify.Builders;

public class TypeDefinitionBuilder
{
	private readonly EditContext _editContext;

	private string _namespace;

	private string _name;

	private TypeAttributes _attributes;

	private MetadataType _metadataType;

	private TypeReference _baseType;

	private TypeDefinition _declaringType;

	private IGenericParameterProvider _cloneGenericParametersFrom;

	private readonly List<MethodDefinitionBuilder> _methods = new List<MethodDefinitionBuilder>();

	private readonly List<FieldDefinitionBuilder> _fields = new List<FieldDefinitionBuilder>();

	private readonly List<PropertyDefinitionBuilder> _properties = new List<PropertyDefinitionBuilder>();

	private readonly List<EventDefinitionBuilder> _events = new List<EventDefinitionBuilder>();

	internal TypeDefinitionBuilder(EditContext context, string @namespace, string name, TypeAttributes attributes, TypeReference baseType, MetadataType metadataType)
	{
		_editContext = context;
		_namespace = @namespace;
		_name = name;
		_attributes = attributes;
		_baseType = baseType;
		_metadataType = metadataType;
	}

	public MethodDefinitionBuilderOnTypeBuilder BuildMethod(string methodName)
	{
		return BuildMethod(methodName, MethodAttributes.Public, _editContext.Context.GetSystemType(SystemType.Void));
	}

	public MethodDefinitionBuilderOnTypeBuilder BuildMethod(string methodName, MethodAttributes attributes, TypeReference returnType)
	{
		MethodDefinitionBuilderOnTypeBuilder builder = new MethodDefinitionBuilderOnTypeBuilder(_editContext, this, methodName, attributes, returnType);
		_methods.Add(builder);
		return builder;
	}

	public FieldDefinitionBuilderOnTypeBuilder BuildField(string fieldName, FieldAttributes attributes, TypeReference propertyType)
	{
		FieldDefinitionBuilderOnTypeBuilder builder = new FieldDefinitionBuilderOnTypeBuilder(_editContext, this, fieldName, attributes, propertyType);
		_fields.Add(builder);
		return builder;
	}

	public PropertyDefinitionBuilder BuildProperty(string propertyName, PropertyAttributes attributes, TypeReference propertyType)
	{
		PropertyDefinitionBuilder builder = new PropertyDefinitionBuilder(_editContext, propertyName, attributes, propertyType);
		_properties.Add(builder);
		return builder;
	}

	public EventDefinitionBuilder BuildEvent(string eventName, EventAttributes attributes, TypeReference eventType)
	{
		EventDefinitionBuilder builder = new EventDefinitionBuilder(_editContext, eventName, attributes, eventType);
		_events.Add(builder);
		return builder;
	}

	public TypeDefinitionBuilder WithDeclaringType(TypeDefinition declaringType)
	{
		_declaringType = declaringType;
		return this;
	}

	public TypeDefinitionBuilder AddGenericParameter(string name)
	{
		throw new NotImplementedException();
	}

	public TypeDefinitionBuilder CloneGenericParameters(IGenericParameterProvider provider)
	{
		if (!provider.HasGenericParameters)
		{
			return this;
		}
		_cloneGenericParametersFrom = provider;
		return this;
	}

	internal TypeDefinition CompleteBuildStage(ITypeFactory typeFactory, RuntimeStorageKind? runtimeStorage = null)
	{
		return Complete(typeFactory, creatingFromBuildStage: true, runtimeStorage);
	}

	public TypeDefinition Complete()
	{
		return Complete(_editContext.Context.CreateThreadSafeFactoryForFullConstruction(), creatingFromBuildStage: false);
	}

	private TypeDefinition Complete(ITypeFactory typeFactory, bool creatingFromBuildStage, RuntimeStorageKind? runtimeStorage = null)
	{
		AssemblyDefinition targetAssembly = ((_declaringType == null) ? _editContext.Context.GeneratedTypesAssembly : _declaringType.Assembly);
		TypeDefinition type = new TypeDefinition(_editContext.Context, targetAssembly.MainModule, _namespace, _name, _declaringType, ReadOnlyCollectionCache<CustomAttribute>.Empty, targetAssembly.IssueNewTypeDefinitionMetadataToken(), _attributes, _metadataType);
		((IAssemblyDefinitionUpdater)targetAssembly).AddGeneratedType(type);
		type.InitializeBaseType(_baseType);
		type.InitializeFields(CompleteFields(type));
		type.InitializeEvents(CompleteEvents(type));
		type.InitializeProperties(CompleteProperties(type));
		type.InitializeNestedTypes(ReadOnlyCollectionCache<TypeDefinition>.Empty);
		type.InitializeInterfaces(ReadOnlyCollectionCache<InterfaceImplementation>.Empty);
		type.InitializeTypeReferenceInterfaceTypes(ReadOnlyCollectionCache<TypeReference>.Empty);
		type.InitializeFieldDuplication();
		if (_cloneGenericParametersFrom == null)
		{
			GenericParameterProviderPopulater.InitializeEmpty(type);
		}
		else
		{
			((IGenericParamProviderInitializer)type).InitializeGenericParameters(_cloneGenericParametersFrom.GenericParameters.Select((GenericParameter gp) => new GenericParameter(gp, type, _editContext.Context)).ToArray().AsReadOnly());
		}
		RuntimeStorageKind typeRuntimeStorage = runtimeStorage ?? TypeRuntimeStorage.GetTypeDefinitionRuntimeStorageKind(type);
		type.InitializeMethods(CompleteMethods(type, typeFactory));
		type.InitializeTypeDefProperties(type.Methods.SingleOrDefault((MethodDefinition m) => m.IsStaticConstructor), DefinitionPopulater.IsGraftedArrayInterfaceType(type), DefinitionPopulater.IsByRefLike(_editContext.Context, type, typeRuntimeStorage), typeRuntimeStorage);
		ReferencePopulater.PopulateTypeRefProperties(type);
		if (_declaringType != null)
		{
			((ITypeDefinitionUpdater)_declaringType).AddNestedType(type);
		}
		if (creatingFromBuildStage)
		{
			return type;
		}
		foreach (GenericParameter genericParameter in type.GenericParameters)
		{
			ReferencePopulater.PopulateTypeRefProperties(genericParameter);
		}
		InitializeMembers(type);
		DefinitionInflater.PopulateTypeDefinitionInflatedProperties(_editContext.Context, _editContext.Context.CreateThreadSafeFactoryForFullConstruction(), type);
		return type;
	}

	private static void InitializeMembers(TypeDefinition type)
	{
		foreach (FieldDefinition field in type.Fields)
		{
			ReferencePopulater.PopulateFieldDefinitionProperties(field);
		}
	}

	private ReadOnlyCollection<MethodDefinition> CompleteMethods(TypeDefinition type, ITypeFactory typeFactory)
	{
		if (_methods.Count == 0)
		{
			return ReadOnlyCollectionCache<MethodDefinition>.Empty;
		}
		List<MethodDefinition> methods = new List<MethodDefinition>(_methods.Count);
		foreach (MethodDefinitionBuilder methodBuilder in _methods)
		{
			methods.Add(methodBuilder.Complete(type, typeFactory, typeUnderConstruction: true, updateInflatedInstances: false));
		}
		return methods.AsReadOnly();
	}

	private ReadOnlyCollection<FieldDefinition> CompleteFields(TypeDefinition type)
	{
		if (_fields.Count == 0)
		{
			return ReadOnlyCollectionCache<FieldDefinition>.Empty;
		}
		List<FieldDefinition> fields = new List<FieldDefinition>(_fields.Count);
		foreach (FieldDefinitionBuilder builder in _fields)
		{
			fields.Add(builder.Complete(type, typeUnderConstruction: true, creatingFromBuildStage: true, updateInflatedInstances: false));
		}
		return fields.AsReadOnly();
	}

	private ReadOnlyCollection<PropertyDefinition> CompleteProperties(TypeDefinition type)
	{
		if (_properties.Count == 0)
		{
			return ReadOnlyCollectionCache<PropertyDefinition>.Empty;
		}
		List<PropertyDefinition> properties = new List<PropertyDefinition>(_properties.Count);
		foreach (PropertyDefinitionBuilder builder in _properties)
		{
			properties.Add(builder.Complete(type, typeUnderConstruction: true));
		}
		return properties.AsReadOnly();
	}

	private ReadOnlyCollection<EventDefinition> CompleteEvents(TypeDefinition type)
	{
		if (_events.Count == 0)
		{
			return ReadOnlyCollectionCache<EventDefinition>.Empty;
		}
		List<EventDefinition> events = new List<EventDefinition>(_events.Count);
		foreach (EventDefinitionBuilder builder in _events)
		{
			events.Add(builder.Complete(type, typeUnderConstruction: true));
		}
		return events.AsReadOnly();
	}
}
