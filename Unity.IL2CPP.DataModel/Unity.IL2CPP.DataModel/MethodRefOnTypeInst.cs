using System;
using System.Collections.ObjectModel;
using System.Threading;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.BuildLogic.Populaters;
using Unity.IL2CPP.DataModel.BuildLogic.Utils;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Generics;

namespace Unity.IL2CPP.DataModel;

public class MethodRefOnTypeInst : MethodSpec
{
	public readonly MethodDefinition MethodDef;

	private LazyInitBool _canShare;

	private MethodReference _sharedMethod;

	private string _fullName;

	private LazyInitBool _hasFullGenericSharingSignature;

	public override MethodAttributes Attributes => MethodDef.Attributes;

	public override MethodImplAttributes ImplAttributes => MethodDef.ImplAttributes;

	public override UnmanagedCallersOnlyInfo UnmanagedCallersOnlyInfo => MethodDef.UnmanagedCallersOnlyInfo;

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

	public override bool ContainsFullySharedGenericTypes => base.DeclaringType.ContainsFullySharedGenericTypes;

	public override bool IsStripped => MethodDef.IsStripped;

	public override bool IsConstructor => MethodDef.IsConstructor;

	public override bool IsStaticConstructor => MethodDef.IsStaticConstructor;

	public override bool HasBody => MethodDef.HasBody;

	public override int CodeSize => MethodDef.CodeSize;

	public override bool IsGenericHiddenMethodNeverUsed => MethodDef.IsGenericHiddenMethodNeverUsed;

	internal override bool RequiresRidForNameUniqueness => MethodDef.RequiresRidForNameUniqueness;

	public override MetadataToken MetadataToken => MethodDef.MetadataToken;

	public override TypeReference ReturnType => MethodDef.ReturnType;

	public override ReadOnlyCollection<ParameterDefinition> Parameters => MethodDef.Parameters;

	internal MethodRefOnTypeInst(GenericInstanceType declaringType, MethodDefinition methodDef)
		: base(declaringType, methodDef.CallingConvention, methodDef.HasThis, methodDef.ExplicitThis, MetadataToken.MethodSpecZero)
	{
		InitializeName(methodDef.Name);
		MethodDef = methodDef;
	}

	public override MethodDefinition Resolve()
	{
		return MethodDef;
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
}
