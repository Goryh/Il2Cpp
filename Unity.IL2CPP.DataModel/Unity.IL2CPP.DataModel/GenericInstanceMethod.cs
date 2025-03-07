using System;
using System.Collections.ObjectModel;
using System.Threading;
using Unity.IL2CPP.DataModel.Awesome;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.BuildLogic.Populaters;
using Unity.IL2CPP.DataModel.BuildLogic.Utils;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Generics;

namespace Unity.IL2CPP.DataModel;

public class GenericInstanceMethod : MethodSpec, IGenericInstance, IMetadataTokenProvider
{
	public readonly MethodDefinition MethodDef;

	private LazyInitBool _containsGenericParameter;

	private string _fullName;

	private LazyInitBool _canShare;

	private LazyInitBool _containsFullGenericSharedTypes;

	private LazyInitBool _hasFullGenericSharingSignature;

	private MethodReference _sharedMethod;

	public GenericInst GenericInst { get; }

	public override bool IsGenericInstance => true;

	public override bool HasBody => MethodDef.HasBody;

	public override int CodeSize => MethodDef.CodeSize;

	public bool HasGenericArguments => GenericInst.Length > 0;

	public int RecursiveGenericDepth => GenericInst.RecursiveGenericDepth;

	public ReadOnlyCollection<TypeReference> GenericArguments => GenericInst.Arguments;

	public override MethodAttributes Attributes => MethodDef.Attributes;

	public override MethodImplAttributes ImplAttributes => MethodDef.ImplAttributes;

	public override bool ContainsGenericParameter
	{
		get
		{
			if (!_containsGenericParameter.IsInitialized)
			{
				_containsGenericParameter.Initialize(LazyMethodPropertyHelpers.ContainsGenericParameters(this));
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

	public override bool ContainsFullySharedGenericTypes
	{
		get
		{
			if (!_containsFullGenericSharedTypes.IsInitialized)
			{
				_containsFullGenericSharedTypes.Initialize(LazyMethodPropertyHelpers.ContainsFullGenericSharedTypes(this));
			}
			return _containsFullGenericSharedTypes.Value;
		}
	}

	public override UnmanagedCallersOnlyInfo UnmanagedCallersOnlyInfo => MethodDef.UnmanagedCallersOnlyInfo;

	public override bool IsStripped => MethodDef.IsStripped;

	public override bool IsConstructor => MethodDef.IsConstructor;

	public override ReadOnlyCollection<ParameterDefinition> Parameters => MethodDef.Parameters;

	internal override bool RequiresRidForNameUniqueness => MethodDef.RequiresRidForNameUniqueness;

	public override MetadataToken MetadataToken => MethodDef.MetadataToken;

	public override TypeReference ReturnType => MethodDef.ReturnType;

	protected override bool IsFullNameBuilt => _fullName != null;

	internal GenericInstanceMethod(TypeReference declaringType, MethodDefinition methodDef, GenericInst genericInst)
		: base(declaringType, methodDef.CallingConvention, methodDef.HasThis, methodDef.ExplicitThis, MetadataToken.MethodSpecZero)
	{
		if (methodDef.GenericParameters.Count != genericInst.Length)
		{
			throw new ArgumentException($"Incorrect number of generic arguments, expected {methodDef.GenericParameters.Count} but was given {genericInst.Length}", "genericInst");
		}
		InitializeName(methodDef.Name);
		MethodDef = methodDef;
		GenericInst = genericInst;
	}

	public override bool CanShare(ITypeFactory typeFactory)
	{
		if (!_canShare.IsInitialized)
		{
			_canShare.Initialize(GenericSharingAnalysis.CanShareMethod(base.DeclaringType.Context, this, typeFactory));
		}
		return _canShare.Value;
	}

	public override MethodReference GetSharedMethod(ITypeFactory typeFactory)
	{
		if (!CanShare(typeFactory))
		{
			throw new ArgumentException($"{this} does not have a shared type");
		}
		if (_sharedMethod == null)
		{
			Interlocked.CompareExchange(ref _sharedMethod, GenericSharingAnalysis.GetSharedMethod(base.DeclaringType.Context, typeFactory, this), null);
		}
		return _sharedMethod;
	}

	public override bool HasFullGenericSharingSignature(ITypeFactory typeFactory)
	{
		if (!_hasFullGenericSharingSignature.IsInitialized)
		{
			_hasFullGenericSharingSignature.Initialize(LazyMethodPropertyHelpers.HasFullGenericSharingSignature(typeFactory, this));
		}
		return _hasFullGenericSharingSignature.Value;
	}

	public override TypeReference GetResolvedThisType(ITypeFactory typeFactory)
	{
		return GenericParameterResolver.ResolveThisTypeIfNeeded(typeFactory, this);
	}

	public override MethodDefinition Resolve()
	{
		return MethodDef;
	}
}
