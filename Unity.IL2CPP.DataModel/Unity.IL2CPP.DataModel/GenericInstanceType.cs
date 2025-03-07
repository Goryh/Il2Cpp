using System;
using System.Collections.ObjectModel;
using System.Threading;
using Unity.IL2CPP.DataModel.BuildLogic.Inflation;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.BuildLogic.Utils;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Generics;
using Unity.IL2CPP.DataModel.RuntimeStorage;

namespace Unity.IL2CPP.DataModel;

public class GenericInstanceType : TypeSpecification, IGenericInstance, IMetadataTokenProvider
{
	public readonly TypeDefinition TypeDef;

	private LazyInitBool _containsGenericParameter;

	private string _fullName;

	private LazyInitBool _canShare;

	private LazyInitBool _containsFullGenericSharedTypes;

	private GenericInstanceType _sharedType;

	private GenericInstanceType _collapsedSignatureType;

	private LazyInitEnum<RuntimeStorageKind> _runtimeStorage;

	private LazyInitEnum<RuntimeFieldLayoutKind> _runtimeFieldLayout;

	private LazyInitEnum<RuntimeFieldLayoutKind> _runtimeStaticFieldLayout;

	private LazyInitEnum<RuntimeFieldLayoutKind> _runtimeThreadStaticFieldLayout;

	public GenericInst GenericInst { get; }

	public override bool IsValueType => TypeDef.IsValueType;

	public override bool IsInterface => TypeDef.IsInterface;

	public override bool IsDelegate => TypeDef.IsDelegate;

	public override bool IsNullableGenericInstance => TypeDef == Context.GetSystemType(SystemType.Nullable);

	public override bool ContainsDefaultInterfaceMethod => TypeDef.ContainsDefaultInterfaceMethod;

	public override bool IsAbstract => TypeDef.IsAbstract;

	public override bool IsGenericInstance => true;

	public override MetadataType MetadataType => MetadataType.GenericInstance;

	public bool HasGenericArguments => GenericInst.Length > 0;

	public int RecursiveGenericDepth => GenericInst.RecursiveGenericDepth;

	public ReadOnlyCollection<TypeReference> GenericArguments => GenericInst.Arguments;

	public override bool ContainsFullySharedGenericTypes
	{
		get
		{
			if (!_containsFullGenericSharedTypes.IsInitialized)
			{
				_containsFullGenericSharedTypes.Initialize(TypeInflationHelpers.ContainsFullGenericSharedTypes(this));
			}
			return _containsFullGenericSharedTypes.Value;
		}
	}

	public override bool ContainsGenericParameter
	{
		get
		{
			if (!_containsGenericParameter.IsInitialized)
			{
				_containsGenericParameter.Initialize(TypeInflationHelpers.ContainsGenericParameters(this));
			}
			return _containsGenericParameter.Value;
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

	protected override bool IsFullNameBuilt => _fullName != null;

	internal GenericInstanceType(TypeDefinition typeDef, TypeReference declaringType, GenericInst genericInst, TypeContext context)
		: base(declaringType, typeDef, context)
	{
		if (typeDef.GenericParameters.Count != genericInst.Length)
		{
			throw new ArgumentException($"Incorrect number of generic arguments, expected {typeDef.GenericParameters.Count} but was given {genericInst.Length}", "genericInst");
		}
		InitializeName(typeDef.Name);
		TypeDef = typeDef;
		GenericInst = genericInst;
	}

	public override bool CanShare(ITypeFactory typeFactory)
	{
		if (!_canShare.IsInitialized)
		{
			_canShare.Initialize(GenericSharingAnalysis.CanShareType(Context, this, typeFactory));
		}
		return _canShare.Value;
	}

	public override bool IsSharedType(ITypeFactory typeFactory)
	{
		if (CanShare(typeFactory))
		{
			return GetSharedType(typeFactory) == this;
		}
		return false;
	}

	public override GenericInstanceType GetSharedType(ITypeFactory typeFactory)
	{
		if (!CanShare(typeFactory))
		{
			throw new ArgumentException($"{this} does not have a shared type");
		}
		if (_sharedType == null)
		{
			Interlocked.CompareExchange(ref _sharedType, GenericSharingAnalysis.GetSharedType(Context, typeFactory, this), null);
		}
		return _sharedType;
	}

	public GenericInstanceType GetCollapsedSignatureType(ITypeFactory typeFactory)
	{
		if (_collapsedSignatureType == null)
		{
			Interlocked.CompareExchange(ref _collapsedSignatureType, GenericSharingAnalysis.GetCollapsedSignatureType(Context, typeFactory, this), null);
		}
		return _collapsedSignatureType;
	}

	public override bool HasAttribute(string @namespace, string name)
	{
		return TypeDef.HasAttribute(@namespace, name);
	}

	public override TypeDefinition Resolve()
	{
		return TypeDef;
	}

	public override RuntimeStorageKind GetRuntimeStorage(ITypeFactory typeFactory)
	{
		if (!_runtimeStorage.IsInitialized)
		{
			SetupRuntimeFieldLayout(typeFactory);
		}
		return _runtimeStorage.Value;
	}

	public override RuntimeFieldLayoutKind GetRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		if (!_runtimeFieldLayout.IsInitialized)
		{
			SetupRuntimeFieldLayout(typeFactory);
		}
		return _runtimeFieldLayout.Value;
	}

	public override RuntimeFieldLayoutKind GetStaticRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		if (!_runtimeStaticFieldLayout.IsInitialized)
		{
			_runtimeStaticFieldLayout.SetValue(TypeRuntimeStorage.StaticFieldLayout(typeFactory, this));
		}
		return _runtimeStaticFieldLayout.Value;
	}

	public override RuntimeFieldLayoutKind GetThreadStaticRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		if (!_runtimeThreadStaticFieldLayout.IsInitialized)
		{
			_runtimeThreadStaticFieldLayout.SetValue(TypeRuntimeStorage.ThreadStaticFieldLayout(typeFactory, this));
		}
		return _runtimeThreadStaticFieldLayout.Value;
	}

	private void SetupRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		var (runtimeStorage, runtimeFieldLayout) = TypeRuntimeStorage.RuntimeStorageKindAndFieldLayout(typeFactory, this);
		_runtimeStorage.SetValue(runtimeStorage);
		_runtimeFieldLayout.SetValue(runtimeFieldLayout);
	}
}
