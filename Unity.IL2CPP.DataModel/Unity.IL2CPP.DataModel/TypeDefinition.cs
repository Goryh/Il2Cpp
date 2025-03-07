using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.BuildLogic.Utils;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Extensions;
using Unity.IL2CPP.DataModel.Modify.Definitions;
using Unity.IL2CPP.DataModel.RuntimeStorage;

namespace Unity.IL2CPP.DataModel;

public class TypeDefinition : TypeReference, ICustomAttributeProvider, IMetadataTokenProvider, IMemberDefinition, ITypeDefinitionUpdater
{
	private TypeReference _baseType;

	private ReadOnlyCollection<FieldDefinition> _fields;

	private ReadOnlyCollection<MethodDefinition> _methods;

	private ReadOnlyCollection<PropertyDefinition> _properties;

	private ReadOnlyCollection<EventDefinition> _events;

	private ReadOnlyCollection<TypeDefinition> _nestedTypes;

	private ReadOnlyCollection<InterfaceImplementation> _interfaces;

	private string _fullName;

	private MethodDefinition _staticConstructor;

	private bool _propertiesInitializedGenericSharing;

	private GenericInstanceType _fullySharedType;

	private bool _hasFullySharableGenericParameters;

	private bool _propertiesInitialized;

	private bool _hasStaticConstructor;

	private bool _isGraftedArrayInterfaceType;

	private bool _isByRefLike;

	private RuntimeStorageKind _runtimeStorage;

	private LazyInitEnum<RuntimeFieldLayoutKind> _runtimeFieldLayout;

	private bool _baseTypeHasBeenUpdated;

	private FieldDuplication _fieldDuplication;

	internal bool IsDataModelGenerated { get; }

	public override bool IsValueType
	{
		get
		{
			if (!_propertiesInitialized)
			{
				ThrowDataNotInitialized("IsValueType");
			}
			if (_runtimeStorage != RuntimeStorageKind.ValueType)
			{
				return _runtimeStorage == RuntimeStorageKind.VariableSizedValueType;
			}
			return true;
		}
	}

	public override string FullName
	{
		get
		{
			if (_fullName == null)
			{
				Interlocked.CompareExchange(ref _fullName, LazyNameHelpers.GetFullName(this), null);
			}
			return _fullName;
		}
	}

	public TypeReference BaseType => _baseType;

	public override bool IsEnum
	{
		get
		{
			if (BaseType != null)
			{
				return BaseType == Context.GetSystemType(SystemType.Enum);
			}
			return false;
		}
	}

	public override bool IsDelegate
	{
		get
		{
			if (BaseType != null)
			{
				return BaseType == Context.GetSystemType(SystemType.MulticastDelegate);
			}
			return false;
		}
	}

	public override bool IsAttribute
	{
		get
		{
			if (this != Context.GetSystemType(SystemType.Attribute))
			{
				if (BaseType != null)
				{
					return BaseType.IsAttribute;
				}
				return false;
			}
			return true;
		}
	}

	public override bool IsIntegralType
	{
		get
		{
			if (!IsSignedIntegralType)
			{
				return IsUnsignedIntegralType;
			}
			return true;
		}
	}

	public override bool ContainsDefaultInterfaceMethod => Methods.Any((MethodDefinition m) => m.IsDefaultInterfaceMethod);

	public override bool IsComInterface
	{
		get
		{
			if (IsInterface && IsImport)
			{
				return !IsWindowsRuntimeProjection;
			}
			return false;
		}
	}

	public override bool IsSignedIntegralType
	{
		get
		{
			if (MetadataType != MetadataType.SByte && MetadataType != MetadataType.Int16 && MetadataType != MetadataType.Int32)
			{
				return MetadataType == MetadataType.Int64;
			}
			return true;
		}
	}

	public override bool IsUnsignedIntegralType
	{
		get
		{
			if (MetadataType != MetadataType.Byte && MetadataType != MetadataType.UInt16 && MetadataType != MetadataType.UInt32)
			{
				return MetadataType == MetadataType.UInt64;
			}
			return true;
		}
	}

	public override bool IsPrimitive => MetadataType.IsPrimitive();

	public bool IsUnicodeClass => Attributes.HasFlag(TypeAttributes.UnicodeClass);

	public override bool HasStaticConstructor
	{
		get
		{
			if (!_propertiesInitialized)
			{
				ThrowDataNotInitialized("HasStaticConstructor");
			}
			return _hasStaticConstructor;
		}
	}

	public MethodDefinition StaticConstructor
	{
		get
		{
			if (!_propertiesInitialized)
			{
				ThrowDataNotInitialized("StaticConstructor");
			}
			return _staticConstructor;
		}
	}

	public override bool IsGraftedArrayInterfaceType
	{
		get
		{
			if (!_propertiesInitialized)
			{
				ThrowDataNotInitialized("IsGraftedArrayInterfaceType");
			}
			return _isGraftedArrayInterfaceType;
		}
	}

	public override bool IsByRefLike
	{
		get
		{
			if (!_propertiesInitialized)
			{
				ThrowDataNotInitialized("IsByRefLike");
			}
			return _isByRefLike;
		}
	}

	public override FieldDuplication FieldDuplication => _fieldDuplication;

	public ReadOnlyCollection<TypeDefinition> NestedTypes
	{
		get
		{
			if (_nestedTypes == null)
			{
				ThrowDataNotInitialized("NestedTypes");
			}
			return _nestedTypes;
		}
	}

	public bool HasInterfaces => Interfaces.Count > 0;

	public bool HasEvents => Events.Count > 0;

	public bool HasMethods => Methods.Count > 0;

	public bool HasProperties => Properties.Count > 0;

	public bool HasFields => Fields.Count > 0;

	public bool HasCustomAttributes => CustomAttributes.Count > 0;

	public bool HasNestedTypes => NestedTypes.Count > 0;

	public TypeAttributes Attributes { get; }

	public override bool IsAbstract => (Attributes & TypeAttributes.Abstract) != 0;

	public bool IsClass => (Attributes & TypeAttributes.ClassSemanticMask) == 0;

	public bool IsImport => (Attributes & TypeAttributes.Import) != 0;

	public override bool IsInterface => (Attributes & TypeAttributes.ClassSemanticMask) == TypeAttributes.ClassSemanticMask;

	public bool IsPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public;

	public bool IsSealed => (Attributes & TypeAttributes.Sealed) != 0;

	public bool IsAutoClass => (Attributes & TypeAttributes.AutoClass) != 0;

	public bool IsAutoLayout => (Attributes & TypeAttributes.LayoutMask) == 0;

	public bool IsExplicitLayout => (Attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout;

	public bool IsSequentialLayout => (Attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout;

	public bool IsWindowsRuntime => (Attributes & TypeAttributes.WindowsRuntime) != 0;

	public bool IsSpecialName => Attributes.HasFlag(TypeAttributes.SpecialName);

	public bool IsRuntimeSpecialName => Attributes.HasFlag(TypeAttributes.RTSpecialName);

	public override bool IsWindowsRuntimeProjection { get; }

	public virtual AssemblyDefinition Assembly { get; }

	public ReadOnlyCollection<FieldDefinition> Fields
	{
		get
		{
			if (_fields == null)
			{
				throw new ArgumentException("Data has not been initialized yet");
			}
			return _fields;
		}
	}

	public ReadOnlyCollection<MethodDefinition> Methods
	{
		get
		{
			if (_methods == null)
			{
				throw new ArgumentException("Data has not been initialized yet");
			}
			return _methods;
		}
	}

	public ReadOnlyCollection<PropertyDefinition> Properties
	{
		get
		{
			if (_properties == null)
			{
				throw new ArgumentException("Data has not been initialized yet");
			}
			return _properties;
		}
	}

	public ReadOnlyCollection<EventDefinition> Events
	{
		get
		{
			if (_events == null)
			{
				throw new ArgumentException("Data has not been initialized yet");
			}
			return _events;
		}
	}

	public ReadOnlyCollection<InterfaceImplementation> Interfaces
	{
		get
		{
			if (_interfaces == null)
			{
				ThrowDataNotInitialized("Interfaces");
			}
			return _interfaces;
		}
	}

	public bool HasFullySharableGenericParameters
	{
		get
		{
			if (!_propertiesInitializedGenericSharing)
			{
				ThrowDataNotInitialized("HasFullySharableGenericParameters");
			}
			return _hasFullySharableGenericParameters;
		}
	}

	public GenericInstanceType FullySharedType
	{
		get
		{
			if (!base.HasGenericParameters)
			{
				throw new NotSupportedException($"Attempting to get a fully shared type for type '{this}' which does not have any generic parameters");
			}
			return _fullySharedType;
		}
	}

	public override bool IsDefinition => true;

	public int ClassSize { get; }

	public short PackingSize { get; }

	public override MetadataType MetadataType { get; }

	public ReadOnlyCollection<CustomAttribute> CustomAttributes { get; }

	protected override bool IsFullNameBuilt => _fullName != null;

	TypeDefinition IMemberDefinition.DeclaringType => (TypeDefinition)base.DeclaringType;

	bool ITypeDefinitionUpdater.BaseTypeHasBeenUpdated => _baseTypeHasBeenUpdated;

	internal TypeDefinition(TypeContext context, ModuleDefinition module, Mono.Cecil.TypeDefinition typeDefinition, TypeDefinition declaringType, ReadOnlyCollection<CustomAttribute> customAttrs)
		: this(context, module, typeDefinition.Namespace, typeDefinition.Name, declaringType, customAttrs, MetadataToken.FromCecil(typeDefinition), typeDefinition.ClassSize, typeDefinition.PackingSize, (TypeAttributes)typeDefinition.Attributes, (MetadataType)typeDefinition.MetadataType, typeDefinition.IsWindowsRuntimeProjection, isDataModelGenerated: false)
	{
		if (typeDefinition == null)
		{
			throw new ArgumentNullException("typeDefinition");
		}
	}

	internal TypeDefinition(TypeContext context, ModuleDefinition module, string @namespace, string name, TypeDefinition declaringType, ReadOnlyCollection<CustomAttribute> customAttrs, MetadataToken token, TypeAttributes typeAttributes, MetadataType metadataType, bool isDataModelGenerated = true)
		: this(context, module, @namespace, name, declaringType, customAttrs, token, -1, -1, typeAttributes, metadataType, isWindowsRuntimeProjection: false, isDataModelGenerated)
	{
	}

	internal TypeDefinition(TypeContext context, ModuleDefinition module, string @namespace, string name, TypeDefinition declaringType, ReadOnlyCollection<CustomAttribute> customAttrs, MetadataToken token, int classSize, short packingSize, TypeAttributes typeAttributes, MetadataType metadataType, bool isWindowsRuntimeProjection, bool isDataModelGenerated = true)
		: base(context, module, declaringType, @namespace, token)
	{
		InitializeName(name);
		CustomAttributes = customAttrs;
		ClassSize = classSize;
		PackingSize = packingSize;
		Attributes = typeAttributes;
		MetadataType = metadataType;
		Assembly = module.Assembly;
		IsWindowsRuntimeProjection = isWindowsRuntimeProjection;
		IsDataModelGenerated = isDataModelGenerated;
	}

	public override RuntimeStorageKind GetRuntimeStorage(ITypeFactory typeFactory)
	{
		if (!_propertiesInitialized)
		{
			ThrowDataNotInitialized("GetRuntimeStorage");
		}
		return _runtimeStorage;
	}

	public override RuntimeFieldLayoutKind GetRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		if (!_runtimeFieldLayout.IsInitialized)
		{
			_runtimeFieldLayout.SetValue(TypeRuntimeStorage.RuntimeFieldLayout(typeFactory, this));
		}
		return _runtimeFieldLayout.Value;
	}

	public override RuntimeFieldLayoutKind GetStaticRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		if (ContainsGenericParameter || IsGenericParameter)
		{
			throw new InvalidOperationException();
		}
		return RuntimeFieldLayoutKind.Fixed;
	}

	public override RuntimeFieldLayoutKind GetThreadStaticRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		if (ContainsGenericParameter || IsGenericParameter)
		{
			throw new InvalidOperationException();
		}
		return RuntimeFieldLayoutKind.Fixed;
	}

	internal void InitializeBaseType(TypeReference baseType)
	{
		_baseType = baseType;
		InitializeTypeReferenceBaseType(baseType);
	}

	internal void InitializeNestedTypes(ReadOnlyCollection<TypeDefinition> nestedTypes)
	{
		_nestedTypes = nestedTypes;
	}

	internal void InitializeFields(ReadOnlyCollection<FieldDefinition> fields)
	{
		_fields = fields;
	}

	internal void InitializeFieldDuplication()
	{
		if (_fields.Count < 2)
		{
			return;
		}
		FieldDefinition[] array = _fields.ToArray();
		Array.Sort(array, (FieldDefinition f1, FieldDefinition f2) => string.CompareOrdinal(f1.Name, f2.Name));
		for (int i = 1; i < array.Length; i++)
		{
			FieldDefinition f3 = array[i - 1];
			FieldDefinition f4 = array[i];
			if (string.CompareOrdinal(f3.Name, f4.Name) == 0 && _fieldDuplication != FieldDuplication.Signatures)
			{
				_fieldDuplication = ((f3.FieldType != f4.FieldType) ? FieldDuplication.Names : FieldDuplication.Signatures);
			}
		}
	}

	internal void InitializeMethods(ReadOnlyCollection<MethodDefinition> methods)
	{
		_methods = methods;
		InitializeTypeReferenceMethods((methods.Count == 0) ? ReadOnlyCollectionCache<MethodReference>.Empty : ((IEnumerable<MethodDefinition>)methods).Select((Func<MethodDefinition, MethodReference>)((MethodDefinition m) => m)).ToArray().AsReadOnly());
	}

	internal void InitializeInterfaces(ReadOnlyCollection<InterfaceImplementation> interfaces)
	{
		_interfaces = interfaces;
	}

	internal void InitializeProperties(ReadOnlyCollection<PropertyDefinition> properties)
	{
		_properties = properties;
	}

	internal void InitializeEvents(ReadOnlyCollection<EventDefinition> events)
	{
		_events = events;
	}

	internal void InitializeTypeDefProperties(MethodDefinition staticConstructor, bool isGraftedArrayInterfaceType, bool isByRefLike, RuntimeStorageKind runtimeStorage)
	{
		_hasStaticConstructor = staticConstructor != null;
		_staticConstructor = staticConstructor;
		_isGraftedArrayInterfaceType = isGraftedArrayInterfaceType;
		_isByRefLike = isByRefLike;
		_runtimeStorage = runtimeStorage;
		_propertiesInitialized = true;
	}

	internal void InitializeGenericSharingProperties(bool hasFullySharableGenericParameters, GenericInstanceType fullySharedType)
	{
		_hasFullySharableGenericParameters = hasFullySharableGenericParameters;
		_fullySharedType = fullySharedType;
		_propertiesInitializedGenericSharing = true;
	}

	public override TypeReference GetUnderlyingEnumType()
	{
		if (!IsEnum)
		{
			throw new NotSupportedException($"{this} is not an emum");
		}
		for (int i = 0; i < Fields.Count; i++)
		{
			FieldDefinition field = Fields[i];
			if (!field.IsStatic)
			{
				return field.FieldType;
			}
		}
		throw new ArgumentException();
	}

	public override bool HasAttribute(string @namespace, string name)
	{
		return CustomAttributeProviderExtensions.HasAttribute(this, @namespace, name);
	}

	public override TypeDefinition Resolve()
	{
		return this;
	}

	void ITypeDefinitionUpdater.AddMethod(MethodDefinition method)
	{
		InitializeMethods(Methods.Append(method).ToArray().AsReadOnly());
	}

	void ITypeDefinitionUpdater.AddEvent(EventDefinition @event)
	{
		InitializeEvents(Events.Append(@event).ToArray().AsReadOnly());
	}

	void ITypeDefinitionUpdater.AddProperty(PropertyDefinition property)
	{
		InitializeProperties(Properties.Append(property).ToArray().AsReadOnly());
	}

	void ITypeDefinitionUpdater.AddField(FieldDefinition field)
	{
		InitializeFields(Fields.Append(field).ToArray().AsReadOnly());
	}

	void ITypeDefinitionUpdater.AddInterfaceImplementations(IEnumerable<InterfaceImplementation> interfaceImplementations)
	{
		InitializeInterfaces(Interfaces.Concat(interfaceImplementations).ToArray().AsReadOnly());
	}

	void ITypeDefinitionUpdater.AddNestedType(TypeDefinition type)
	{
		InitializeNestedTypes(NestedTypes.Append(type).ToArray().AsReadOnly());
	}

	void ITypeDefinitionUpdater.UpdateBaseType(TypeReference newBaseType)
	{
		InitializeBaseType(newBaseType);
		_baseTypeHasBeenUpdated = true;
	}

	public bool IsReferenceToThisTypeDefinition(TypeReference typeReference)
	{
		if (typeReference == this)
		{
			return true;
		}
		if (typeReference.Resolve() != this)
		{
			return false;
		}
		if (!(typeReference is GenericInstanceType genericInstanceType))
		{
			return false;
		}
		for (int i = 0; i < genericInstanceType.GenericArguments.Count; i++)
		{
			if (!(genericInstanceType.GenericArguments[i] is GenericParameter gp) || gp.Owner != this || gp.Position != i)
			{
				return false;
			}
		}
		return true;
	}
}
