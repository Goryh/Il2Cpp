using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using Mono.Cecil;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Modify.Definitions;

namespace Unity.IL2CPP.DataModel;

[DebuggerDisplay("{FullName}")]
public class MethodDefinition : MethodReference, IMemberDefinition, ICustomAttributeProvider, IMetadataTokenProvider, IMethodDefinitionUpdater
{
	internal readonly Mono.Cecil.MethodDefinition Definition;

	private readonly MethodImplAttributes _implAttributes;

	private MethodAttributes _attributes;

	private MethodBody _body;

	private ReadOnlyCollection<MethodReference> _overrides;

	private bool _propertiesInitializedGenericSharing;

	private MethodReference _fullySharedMethod;

	private bool _methodAndTypeHaveFullySharableGenericParameters;

	private bool _hasFullySharableGenericParameters;

	private string _fullName;

	private TypeReference _returnType;

	private readonly object _pinvokeOrUnmanagedCallersOnlyInfo;

	private bool _isStripped;

	private bool _isConstructor;

	internal override bool IsDataModelGenerated { get; }

	internal override bool RequiresRidForNameUniqueness { get; }

	public override bool IsDefinition => true;

	public MethodBody Body => _body;

	public override bool HasBody => _body != null;

	public override int CodeSize
	{
		get
		{
			if (!HasBody)
			{
				return 0;
			}
			return Body.CodeSize;
		}
	}

	public override MethodAttributes Attributes => _attributes;

	public override MethodImplAttributes ImplAttributes => _implAttributes;

	public ReadOnlyCollection<CustomAttribute> CustomAttributes { get; }

	public override MethodReturnType MethodReturnType { get; }

	public MethodDebugInfo DebugInformation { get; private set; }

	public bool HasOverrides => Overrides.Count > 0;

	public override ReadOnlyCollection<ParameterDefinition> Parameters { get; }

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

	public override TypeReference ReturnType
	{
		get
		{
			if (_returnType == null)
			{
				ThrowDataNotInitialized("ReturnType");
			}
			return _returnType;
		}
	}

	public ReadOnlyCollection<MethodReference> Overrides
	{
		get
		{
			if (_overrides == null)
			{
				ThrowDataNotInitialized("Overrides");
			}
			return _overrides;
		}
	}

	public bool MethodAndTypeHaveFullySharableGenericParameters
	{
		get
		{
			if (!_propertiesInitializedGenericSharing)
			{
				ThrowDataNotInitialized("MethodAndTypeHaveFullySharableGenericParameters");
			}
			return _methodAndTypeHaveFullySharableGenericParameters;
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

	public bool HasFullySharedMethod
	{
		get
		{
			if (!base.HasGenericParameters)
			{
				return DeclaringType.HasGenericParameters;
			}
			return true;
		}
	}

	public MethodReference FullySharedMethod
	{
		get
		{
			if (!_propertiesInitializedGenericSharing)
			{
				ThrowDataNotInitialized("FullySharedMethod");
			}
			if (!HasFullySharedMethod)
			{
				throw new NotSupportedException($"Attempting to get a fully shared method for '{this}' which does not have any generic parameters");
			}
			return _fullySharedMethod;
		}
	}

	public new TypeDefinition DeclaringType => (TypeDefinition)base.DeclaringType;

	public override bool IsStaticConstructor
	{
		get
		{
			if (IsStatic)
			{
				return IsConstructor;
			}
			return false;
		}
	}

	public override bool IsWindowsRuntimeProjection { get; }

	public override bool IsStripped => _isStripped;

	public override bool IsConstructor => _isConstructor;

	public bool HasPInvokeInfo => PInvokeInfo != null;

	public PInvokeInfo PInvokeInfo => _pinvokeOrUnmanagedCallersOnlyInfo as PInvokeInfo;

	public override UnmanagedCallersOnlyInfo UnmanagedCallersOnlyInfo => _pinvokeOrUnmanagedCallersOnlyInfo as UnmanagedCallersOnlyInfo;

	protected override bool IsFullNameBuilt => _fullName != null;

	internal MethodDefinition(TypeDefinition declaringType, ReadOnlyCollection<ParameterDefinition> parameters, ReadOnlyCollection<CustomAttribute> customAttributes, MethodReturnType methodReturnType, bool requiresRidForNameUniqueness, Mono.Cecil.MethodDefinition definition)
		: this(definition.Name, declaringType, parameters, customAttributes, methodReturnType, (MethodAttributes)definition.Attributes, (MethodImplAttributes)definition.ImplAttributes, (MethodCallingConvention)definition.CallingConvention, definition.HasThis, MetadataToken.FromCecil(definition), PInvokeInfo.FromCecil(definition), UnmanagedCallersOnlyInfo.FromCecil(definition), definition.IsWindowsRuntimeProjection, requiresRidForNameUniqueness, isDataModelGenerated: false)
	{
		Definition = definition;
		DebugInformation = MethodDebugInfo.FromCecil(this, definition.DebugInformation);
	}

	internal MethodDefinition(string name, TypeDefinition declaringType, ReadOnlyCollection<ParameterDefinition> parameters, ReadOnlyCollection<CustomAttribute> customAttributes, MethodReturnType methodReturnType, MethodAttributes attributes, MethodImplAttributes methodImplAttributes, MethodCallingConvention callingConvention, bool hasThis, MetadataToken token, PInvokeInfo pInvokeInfo, UnmanagedCallersOnlyInfo unmanagedCallersOnlyInfo, bool isWindowsRuntimeProjection, bool requiresRidForNameUniqueness, bool isDataModelGenerated = true)
		: base(declaringType, callingConvention, hasThis, explicitThis: false, token)
	{
		InitializeName(name);
		CustomAttributes = customAttributes;
		MethodReturnType = methodReturnType;
		Parameters = parameters;
		if (pInvokeInfo != null)
		{
			_pinvokeOrUnmanagedCallersOnlyInfo = pInvokeInfo;
		}
		if (unmanagedCallersOnlyInfo != null)
		{
			_pinvokeOrUnmanagedCallersOnlyInfo = unmanagedCallersOnlyInfo;
		}
		_attributes = attributes;
		_implAttributes = methodImplAttributes;
		RequiresRidForNameUniqueness = requiresRidForNameUniqueness;
		IsDataModelGenerated = isDataModelGenerated;
		IsWindowsRuntimeProjection = isWindowsRuntimeProjection;
		if (pInvokeInfo != null && unmanagedCallersOnlyInfo != null)
		{
			throw new InvalidProgramException($"A method cannot be used for both P/Invoke and UnmanagedCallersOnly - {declaringType.Namespace}.{declaringType.Name}::{name}");
		}
	}

	public override ReadOnlyCollection<ParameterDefinition> GetResolvedParameters(ITypeFactory typeFactory)
	{
		return Parameters;
	}

	internal void InitializeMethodBody(MethodBody methodBody)
	{
		_body = methodBody;
	}

	internal void InitializeDebugInfo(MethodDebugInfo methodDebugInfo)
	{
		DebugInformation = methodDebugInfo;
	}

	internal void InitializeReturnType(TypeReference returnType)
	{
		_returnType = returnType;
	}

	internal void InitializeOverrides(ReadOnlyCollection<MethodReference> overrides)
	{
		_overrides = overrides;
	}

	public override TypeReference GetResolvedReturnType(ITypeFactory typeFactory)
	{
		return _returnType;
	}

	public override TypeReference GetResolvedThisType(ITypeFactory typeFactory)
	{
		return DeclaringType;
	}

	public override MethodDefinition Resolve()
	{
		return this;
	}

	internal void InitializeGenericSharingProperties(bool hasFullySharableGenericParameters, bool methodAndTypeHaveFullySharableGenericParameters, MethodReference fullySharedMethod)
	{
		_hasFullySharableGenericParameters = hasFullySharableGenericParameters;
		_methodAndTypeHaveFullySharableGenericParameters = methodAndTypeHaveFullySharableGenericParameters;
		_fullySharedMethod = fullySharedMethod;
		_propertiesInitializedGenericSharing = true;
	}

	internal void InitializeMethodDefProperties(bool isStripped, bool isConstructor)
	{
		_isStripped = isStripped;
		_isConstructor = isConstructor;
	}

	void IMethodDefinitionUpdater.UpdateAttributes(MethodAttributes attributes)
	{
		_attributes = attributes;
	}

	public bool HasAttribute(string @namespace, string name)
	{
		return CustomAttributeProviderExtensions.HasAttribute(this, @namespace, name);
	}

	public bool IsReferenceToThisMethodDefinition(MethodReference methodReference)
	{
		if (methodReference == this)
		{
			return true;
		}
		if (methodReference.Resolve() != this)
		{
			return false;
		}
		if (methodReference is GenericInstanceMethod genericInstanceMethod)
		{
			for (int i = 0; i < genericInstanceMethod.GenericArguments.Count; i++)
			{
				if (!(genericInstanceMethod.GenericArguments[i] is GenericParameter gp) || gp.Owner != this || gp.Position != i)
				{
					return false;
				}
			}
		}
		return DeclaringType.IsReferenceToThisTypeDefinition(methodReference.DeclaringType);
	}
}
