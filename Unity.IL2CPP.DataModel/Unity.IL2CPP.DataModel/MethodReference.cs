using System.Collections.ObjectModel;
using System.Threading;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public abstract class MethodReference : MemberReference, IGenericParameterProvider, IMethodSignature, IGenericParamProviderInitializer
{
	private bool _propertiesInitialized;

	private bool _isFinalizerMethod;

	private string _uniqueName;

	private string _uniqueHash;

	private string _cppName;

	private ReadOnlyCollection<GenericParameter> _genericParameters;

	internal virtual bool IsDataModelGenerated => false;

	internal abstract bool RequiresRidForNameUniqueness { get; }

	public bool HasThis { get; }

	public bool ExplicitThis { get; }

	public MethodCallingConvention CallingConvention { get; }

	public bool HasParameters => Parameters.Count > 0;

	public abstract TypeReference ReturnType { get; }

	public abstract ReadOnlyCollection<ParameterDefinition> Parameters { get; }

	public abstract MethodAttributes Attributes { get; }

	public abstract MethodImplAttributes ImplAttributes { get; }

	public virtual bool IsCompilerControlled => (Attributes & MethodAttributes.MemberAccessMask) == 0;

	public bool IsPrivate => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;

	public bool IsFamilyAndAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem;

	public bool IsAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly;

	public bool IsFamily => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family;

	public bool IsFamilyOrAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem;

	public bool IsPublic => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;

	public virtual bool IsStatic => Attributes.HasFlag(MethodAttributes.Static);

	public bool IsFinal => Attributes.HasFlag(MethodAttributes.Final);

	public bool IsVirtual => Attributes.HasFlag(MethodAttributes.Virtual);

	public bool IsHideBySig => Attributes.HasFlag(MethodAttributes.HideBySig);

	public bool IsReuseSlot => (Attributes & MethodAttributes.VtableLayoutMask) == 0;

	public bool IsNewSlot => (Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.VtableLayoutMask;

	public bool IsCheckAccessOnOverride => Attributes.HasFlag(MethodAttributes.CheckAccessOnOverride);

	public bool IsAbstract => Attributes.HasFlag(MethodAttributes.Abstract);

	public bool IsSpecialName => Attributes.HasFlag(MethodAttributes.SpecialName);

	public bool IsPInvokeImpl => Attributes.HasFlag(MethodAttributes.PInvokeImpl);

	public bool IsUnmanagedExport => Attributes.HasFlag(MethodAttributes.UnmanagedExport);

	public bool IsRuntimeSpecialName => Attributes.HasFlag(MethodAttributes.RTSpecialName);

	public bool IsIL => (ImplAttributes & MethodImplAttributes.CodeTypeMask) == 0;

	public bool IsNative => (ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Native;

	public bool IsRuntime => (ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.CodeTypeMask;

	public bool IsUnmanaged => (ImplAttributes & MethodImplAttributes.ManagedMask) == MethodImplAttributes.ManagedMask;

	public bool IsManaged => (ImplAttributes & MethodImplAttributes.ManagedMask) == 0;

	public bool IsForwardRef => ImplAttributes.HasFlag(MethodImplAttributes.ForwardRef);

	public bool IsPreserveSig => ImplAttributes.HasFlag(MethodImplAttributes.PreserveSig);

	public bool IsInternalCall => ImplAttributes.HasFlag(MethodImplAttributes.InternalCall);

	public bool IsSynchronized => ImplAttributes.HasFlag(MethodImplAttributes.Synchronized);

	public bool NoInlining => ImplAttributes.HasFlag(MethodImplAttributes.NoInlining);

	public bool NoOptimization => ImplAttributes.HasFlag(MethodImplAttributes.NoOptimization);

	public bool AggressiveInlining => ImplAttributes.HasFlag(MethodImplAttributes.AggressiveInlining);

	public abstract bool IsStripped { get; }

	public bool IsFinalizerMethod
	{
		get
		{
			if (!_propertiesInitialized)
			{
				ThrowDataNotInitialized("IsFinalizerMethod");
			}
			return _isFinalizerMethod;
		}
	}

	internal string UniqueName
	{
		get
		{
			if (_uniqueName == null)
			{
				Interlocked.CompareExchange(ref _uniqueName, UniqueNameBuilder.GetMethodDefinitionAssemblyQualifiedName(this), null);
			}
			return _uniqueName;
		}
	}

	public bool HasGenericParameters => _genericParameters.Count > 0;

	public GenericParameterType GenericParameterType => GenericParameterType.Method;

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

	public string CppName
	{
		get
		{
			if (_cppName == null)
			{
				Interlocked.CompareExchange(ref _cppName, CppNamePopulator.GetMethodRefCppName(this), null);
			}
			return _cppName;
		}
	}

	public string UniqueHash
	{
		get
		{
			if (_uniqueHash == null)
			{
				Interlocked.CompareExchange(ref _uniqueHash, CppNamePopulator.GenerateForString(UniqueName), null);
			}
			return _uniqueHash;
		}
	}

	public virtual bool IsStaticConstructor => false;

	public abstract bool IsConstructor { get; }

	public abstract bool HasBody { get; }

	public abstract int CodeSize { get; }

	public virtual bool IsGenericInstance => false;

	public bool IsDefaultInterfaceMethod
	{
		get
		{
			if (!IsAbstract)
			{
				return base.DeclaringType.IsInterface;
			}
			return false;
		}
	}

	public virtual bool ContainsFullySharedGenericTypes => false;

	public virtual MethodReturnType MethodReturnType => new MethodReturnType(ReturnType);

	public bool IsUnmanagedCallersOnly => UnmanagedCallersOnlyInfo != null;

	public abstract UnmanagedCallersOnlyInfo UnmanagedCallersOnlyInfo { get; }

	protected MethodReference(TypeReference declaringType, MethodCallingConvention callingConvention, bool hasThis, bool explicitThis, MetadataToken metadataToken)
		: base(declaringType, metadataToken)
	{
		CallingConvention = callingConvention;
		HasThis = hasThis;
		ExplicitThis = explicitThis;
	}

	public abstract ReadOnlyCollection<ParameterDefinition> GetResolvedParameters(ITypeFactory typeFactory);

	public abstract TypeReference GetResolvedReturnType(ITypeFactory typeFactory);

	public abstract TypeReference GetResolvedThisType(ITypeFactory typeFactory);

	internal void InitializeMethodRefProperties(bool isFinalizerMethod)
	{
		_isFinalizerMethod = isFinalizerMethod;
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

	public virtual bool CanShare(ITypeFactory typeFactory)
	{
		return false;
	}

	public virtual MethodReference GetSharedMethod(ITypeFactory typeFactory)
	{
		return this;
	}

	public bool IsSharedMethod(ITypeFactory typeFactory)
	{
		if (CanShare(typeFactory))
		{
			return GetSharedMethod(typeFactory) == this;
		}
		return false;
	}

	public virtual bool HasFullGenericSharingSignature(ITypeFactory typeFactory)
	{
		return false;
	}

	public abstract MethodDefinition Resolve();
}
