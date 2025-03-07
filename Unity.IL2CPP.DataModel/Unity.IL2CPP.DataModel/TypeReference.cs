using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Unity.IL2CPP.DataModel.BuildLogic.Inflation;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.BuildLogic.Utils;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.InjectedInitialize;
using Unity.IL2CPP.DataModel.Modify.Definitions;

namespace Unity.IL2CPP.DataModel;

public abstract class TypeReference : MemberReference, IGenericParameterProvider, IGenericParamProviderInitializer, ITypeReferenceUpdater, ITypeReferenceInjectedInitialize
{
	internal readonly TypeContext Context;

	private bool _propertiesInitialized;

	private bool _hasActivationFactories;

	private bool _isStringBuilder;

	private LazyInitBool _isComOrWindowsRuntimeInterface;

	private string _uniqueName;

	private TypeReference _withoutModifiers;

	private TypeReference _underlyingType;

	private string _cppName;

	private string _cppNameForVariable;

	private string _uniqueHash;

	private ReadOnlyCollection<GenericParameter> _genericParameters;

	private ReadOnlyCollection<MethodReference> _methods;

	private ReadOnlyCollection<InflatedFieldType> _inflatedFieldTypes;

	private ReadOnlyCollection<TypeReference> _interfaceTypes;

	private TypeReference _baseType;

	private object _cppDeclarationsData;

	private int _cppDeclarationsDepth = -1;

	private object _cppDeclarationsDependencies;

	public ReadOnlyCollection<GenericParameter> GenericParameters
	{
		get
		{
			if (_genericParameters == null)
			{
				ThrowDataNotInitialized("GenericParameters");
			}
			return _genericParameters;
		}
	}

	public string Namespace { get; }

	public bool IsCoreLibraryType => Module.Assembly == Context.SystemAssembly;

	public override ModuleDefinition Module { get; }

	public abstract bool IsValueType { get; }

	public virtual bool IsSentinel => false;

	public virtual bool IsPinned => false;

	public virtual bool IsPointer => false;

	public virtual bool IsPrimitive => false;

	public virtual bool IsByReference => false;

	public virtual bool IsFunctionPointer => false;

	public virtual bool IsOptionalModifier => false;

	public virtual bool IsRequiredModifier => false;

	public virtual bool IsGenericInstance => false;

	public virtual bool IsGenericParameter => false;

	public virtual bool IsArray => false;

	public virtual bool HasStaticConstructor => false;

	public abstract bool IsGraftedArrayInterfaceType { get; }

	public abstract bool IsByRefLike { get; }

	public abstract FieldDuplication FieldDuplication { get; }

	public bool HasGenericParameters => GenericParameters.Count > 0;

	public GenericParameterType GenericParameterType => GenericParameterType.Type;

	public bool IsSystemArray => this == Context.GetSystemType(SystemType.Array);

	public abstract MetadataType MetadataType { get; }

	public virtual bool IsDelegate => false;

	public virtual bool IsEnum => false;

	public bool IsSystemEnum => this == Context.GetSystemType(SystemType.Enum);

	public bool IsSystemValueType => this == Context.GetSystemType(SystemType.ValueType);

	public bool IsString => this == Context.GetSystemType(SystemType.String);

	public virtual bool IsAttribute => false;

	public virtual bool IsInterface => false;

	public virtual bool IsNullableGenericInstance => false;

	public abstract bool ContainsDefaultInterfaceMethod { get; }

	public bool IsVoid => WithoutModifiers() == Context.GetSystemType(SystemType.Void);

	public bool IsNotVoid => WithoutModifiers() != Context.GetSystemType(SystemType.Void);

	public virtual bool IsComInterface => false;

	public bool IsSystemObject => this == Context.GetSystemType(SystemType.Object);

	public virtual bool IsIntegralType => false;

	public virtual bool IsSignedIntegralType => false;

	public virtual bool IsUnsignedIntegralType => false;

	public abstract bool IsAbstract { get; }

	public bool IsSystemType => this == Context.GetSystemType(SystemType.Type);

	public bool IsIntegralPointerType
	{
		get
		{
			if (MetadataType != MetadataType.IntPtr)
			{
				return MetadataType == MetadataType.UIntPtr;
			}
			return true;
		}
	}

	public bool IsNested => base.DeclaringType != null;

	internal string UniqueName
	{
		get
		{
			if (_uniqueName == null)
			{
				Interlocked.CompareExchange(ref _uniqueName, UniqueNameBuilder.GetAssemblyQualifiedName(this), null);
			}
			return _uniqueName;
		}
	}

	public string CppName
	{
		get
		{
			if (_cppName == null)
			{
				Interlocked.CompareExchange(ref _cppName, CppNamePopulator.GetTypeRefCppName(this), null);
			}
			return _cppName;
		}
	}

	public virtual string CppNameForVariable
	{
		get
		{
			if (_cppNameForVariable == null)
			{
				Interlocked.CompareExchange(ref _cppNameForVariable, CppNamePopulator.ForVariable(this), null);
			}
			return _cppNameForVariable;
		}
	}

	public string CppNameForPointerToVariable => CppNameForVariable + "*";

	public string CppNameForReferenceToVariable => CppNameForVariable + "*";

	public string UniqueHash
	{
		get
		{
			if (_uniqueHash == null)
			{
				Interlocked.CompareExchange(ref _uniqueHash, CppNamePopulator.GetTypeRefUniqueHash(this), null);
			}
			return _uniqueHash;
		}
	}

	public bool HasActivationFactories
	{
		get
		{
			if (!_propertiesInitialized)
			{
				ThrowDataNotInitialized("HasActivationFactories");
			}
			return _hasActivationFactories;
		}
	}

	public bool IsStringBuilder
	{
		get
		{
			if (!_propertiesInitialized)
			{
				ThrowDataNotInitialized("IsStringBuilder");
			}
			return _isStringBuilder;
		}
	}

	public bool IsIActivationFactory => this == Context.GetIl2CppCustomType(Il2CppCustomType.IActivationFactory);

	public bool IsIl2CppComDelegate => this == Context.GetIl2CppCustomType(Il2CppCustomType.Il2CppComDelegate);

	public bool IsIl2CppComObject => this == Context.GetIl2CppCustomType(Il2CppCustomType.Il2CppComObject);

	public bool IsIl2CppFullySharedGenericType
	{
		get
		{
			if (this != Context.GetIl2CppCustomType(Il2CppCustomType.Il2CppFullySharedGeneric))
			{
				return this == Context.GetIl2CppCustomType(Il2CppCustomType.Il2CppFullySharedGenericStruct);
			}
			return true;
		}
	}

	public virtual bool ContainsFullySharedGenericTypes => IsIl2CppFullySharedGenericType;

	protected TypeReference(TypeContext context, ModuleDefinition module, TypeReference declaringType, string @namespace, MetadataToken metadataToken)
		: base(declaringType, metadataToken)
	{
		Context = context;
		Namespace = @namespace;
		Module = module;
		_baseType = this;
	}

	public IEnumerable<LazilyInflatedMethod> IterateLazilyInflatedMethods(ITypeFactory typeFactory)
	{
		if (_methods != null)
		{
			foreach (MethodReference m in GetMethods(typeFactory))
			{
				yield return new LazilyInflatedMethod(m);
			}
			yield break;
		}
		ReadOnlyCollection<MethodDefinition> definitionMethods = Resolve().Methods;
		if (definitionMethods.Count == 0)
		{
			yield break;
		}
		TypeResolver typeResolver = typeFactory.ResolverFor(this);
		foreach (MethodDefinition method in definitionMethods)
		{
			yield return new LazilyInflatedMethod(this, method, typeResolver);
		}
	}

	public virtual ReadOnlyCollection<MethodReference> GetMethods(ITypeFactory typeFactory)
	{
		if (_methods == null)
		{
			Interlocked.CompareExchange(ref _methods, TypeInflationHelpers.GetMethods(this, typeFactory), null);
		}
		return _methods;
	}

	public virtual ReadOnlyCollection<InflatedFieldType> GetInflatedFieldTypes(ITypeFactory typeFactory)
	{
		if (_inflatedFieldTypes == null)
		{
			Interlocked.CompareExchange(ref _inflatedFieldTypes, TypeInflationHelpers.GetFieldTypes(this, typeFactory), null);
		}
		return _inflatedFieldTypes;
	}

	public virtual TypeReference GetBaseType(ITypeFactory typeFactory)
	{
		if (_baseType == this)
		{
			Interlocked.CompareExchange(ref _baseType, TypeInflationHelpers.GetBaseType(this, typeFactory), this);
		}
		return _baseType;
	}

	public virtual ReadOnlyCollection<TypeReference> GetInterfaceTypes(ITypeFactory typeFactory)
	{
		if (_interfaceTypes == null)
		{
			Interlocked.CompareExchange(ref _interfaceTypes, TypeInflationHelpers.GetInterfaces(this, typeFactory), null);
		}
		return _interfaceTypes;
	}

	object ITypeReferenceInjectedInitialize.GetCppDeclarationsData<TContext>(TContext context, Func<TContext, TypeReference, object> initialize)
	{
		if (_cppDeclarationsData == null)
		{
			Interlocked.CompareExchange(ref _cppDeclarationsData, initialize(context, this), null);
		}
		return _cppDeclarationsData;
	}

	object ITypeReferenceInjectedInitialize.GetCppDeclarationsDependencies<TContext>(TContext context, Func<TContext, TypeReference, object> initialize)
	{
		if (_cppDeclarationsDependencies == null)
		{
			Interlocked.CompareExchange(ref _cppDeclarationsDependencies, initialize(context, this), null);
		}
		return _cppDeclarationsDependencies;
	}

	int ITypeReferenceInjectedInitialize.GetCppDeclarationsDepth<TContext>(TContext context, Func<TContext, TypeReference, int> initialize)
	{
		if (_cppDeclarationsDepth == -1)
		{
			Interlocked.CompareExchange(ref _cppDeclarationsDepth, initialize(context, this), -1);
		}
		return _cppDeclarationsDepth;
	}

	public bool IsComOrWindowsRuntimeInterface(ITypeFactory typeFactory)
	{
		if (!_isComOrWindowsRuntimeInterface.IsInitialized)
		{
			_isComOrWindowsRuntimeInterface.Initialize(TypeInflationHelpers.IsComOrWindowsRuntimeInterface(this, typeFactory));
		}
		return _isComOrWindowsRuntimeInterface.Value;
	}

	public virtual bool IsSharedType(ITypeFactory typeFactory)
	{
		return false;
	}

	public virtual bool CanShare(ITypeFactory typeFactory)
	{
		return false;
	}

	public virtual GenericInstanceType GetSharedType(ITypeFactory typeFactory)
	{
		throw new NotSupportedException();
	}

	internal void InitializeTypeReferenceMethods(ReadOnlyCollection<MethodReference> methods)
	{
		_methods = methods;
	}

	internal void InitializeTypeReferenceFieldTypes(ReadOnlyCollection<InflatedFieldType> fieldTypes)
	{
		_inflatedFieldTypes = fieldTypes;
	}

	internal void InitializeTypeReferenceBaseType(TypeReference baseType)
	{
		_baseType = baseType;
	}

	internal void InitializeTypeReferenceInterfaceTypes(ReadOnlyCollection<TypeReference> interfaceTypes)
	{
		_interfaceTypes = interfaceTypes;
	}

	internal void InitializeTypeRefProperties(bool hasActivationFactory, bool isStringBuilder)
	{
		_hasActivationFactories = hasActivationFactory;
		_isStringBuilder = isStringBuilder;
		_propertiesInitialized = true;
	}

	void IGenericParamProviderInitializer.InitializeGenericParameters(ReadOnlyCollection<GenericParameter> genericParameters)
	{
		if (_genericParameters != null)
		{
			ThrowAlreadyInitializedDataException("_genericParameters");
		}
		_genericParameters = genericParameters;
	}

	public TypeReference WithoutModifiers()
	{
		if (_withoutModifiers == null)
		{
			Interlocked.CompareExchange(ref _withoutModifiers, TypeInflationHelpers.WithoutModifiers(this), null);
		}
		return _withoutModifiers;
	}

	public TypeReference UnderlyingType()
	{
		if (_underlyingType == null)
		{
			Interlocked.CompareExchange(ref _underlyingType, TypeInflationHelpers.UnderlyingType(this), null);
		}
		return _underlyingType;
	}

	public TypeReference GetNonPinnedAndNonByReferenceType()
	{
		TypeReference actualType;
		TypeReference typeReference = (actualType = WithoutModifiers());
		if (typeReference is ByReferenceType byRefType)
		{
			actualType = byRefType.ElementType;
		}
		if (typeReference is PinnedType pinnedType)
		{
			actualType = pinnedType.ElementType;
		}
		return actualType;
	}

	public virtual AssemblyNameReference GetAssemblyNameReference()
	{
		return TypeReferenceGetAssemblyNameReference();
	}

	protected AssemblyNameReference TypeReferenceGetAssemblyNameReference()
	{
		return Module?.Assembly?.Name;
	}

	public virtual TypeReference GetElementType()
	{
		return this;
	}

	public virtual TypeReference GetUnderlyingEnumType()
	{
		throw new NotSupportedException();
	}

	public bool IsUserDefinedStruct()
	{
		if (IsValueType && !IsPrimitive && !IsEnum)
		{
			return !IsVoid;
		}
		return false;
	}

	public virtual bool HasAttribute(string @namespace, string name)
	{
		return false;
	}

	public virtual TypeDefinition Resolve()
	{
		return null;
	}

	void ITypeReferenceUpdater.ClearInterfaceTypesCache()
	{
		_interfaceTypes = null;
	}

	void ITypeReferenceUpdater.ClearMethodsCache()
	{
		_methods = null;
	}

	void ITypeReferenceUpdater.ClearFieldTypes()
	{
		_inflatedFieldTypes = null;
	}

	public abstract RuntimeStorageKind GetRuntimeStorage(ITypeFactory typeFactory);

	public virtual RuntimeFieldLayoutKind GetRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		return RuntimeFieldLayoutKind.Fixed;
	}

	public virtual RuntimeFieldLayoutKind GetStaticRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		return RuntimeFieldLayoutKind.Fixed;
	}

	public virtual RuntimeFieldLayoutKind GetThreadStaticRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		return RuntimeFieldLayoutKind.Fixed;
	}
}
