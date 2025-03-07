using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.BuildLogic.Utils;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public class FunctionPointerType : TypeSpecification, IMethodSignature
{
	private string _fullName;

	private LazyInitBool _containsGenericParameter;

	public override bool IsValueType => false;

	public override bool IsFunctionPointer => true;

	public override MetadataType MetadataType => MetadataType.FunctionPointer;

	public override bool IsGraftedArrayInterfaceType => false;

	public override bool IsEnum => false;

	public override bool IsInterface => false;

	public override bool IsDelegate => false;

	public override bool IsComInterface => false;

	public override bool IsAttribute => false;

	public override bool HasStaticConstructor => false;

	public override bool IsAbstract => false;

	public bool HasThis { get; }

	public bool ExplicitThis { get; }

	public bool HasParameters => Parameters.Count != 0;

	public MethodCallingConvention CallingConvention { get; }

	public TypeReference ReturnType { get; }

	public ReadOnlyCollection<ParameterDefinition> Parameters { get; }

	public override bool ContainsGenericParameter
	{
		get
		{
			if (!_containsGenericParameter.IsInitialized)
			{
				_containsGenericParameter.Initialize(ReturnType.ContainsGenericParameter || Parameters.Any((ParameterDefinition p) => p.ParameterType.ContainsGenericParameter));
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

	public TypeReference TypeForReflectionUsage => Context.GetSystemType(SystemType.IntPtr);

	protected override bool IsFullNameBuilt => _fullName != null;

	internal FunctionPointerType(TypeReference returnType, ReadOnlyCollection<ParameterDefinition> parameters, MethodCallingConvention callingConvention, bool hasThis, bool explicitThis, TypeContext context)
		: base((ModuleDefinition)null, context)
	{
		ReturnType = returnType;
		Parameters = parameters;
		CallingConvention = callingConvention;
		HasThis = hasThis;
		ExplicitThis = explicitThis;
		InitializeName("method");
	}

	public override ReadOnlyCollection<MethodReference> GetMethods(ITypeFactory typeFactory)
	{
		return ReadOnlyCollectionCache<MethodReference>.Empty;
	}

	public override ReadOnlyCollection<InflatedFieldType> GetInflatedFieldTypes(ITypeFactory typeFactory)
	{
		return ReadOnlyCollectionCache<InflatedFieldType>.Empty;
	}

	public override ReadOnlyCollection<TypeReference> GetInterfaceTypes(ITypeFactory typeFactory)
	{
		return ReadOnlyCollectionCache<TypeReference>.Empty;
	}

	public override AssemblyNameReference GetAssemblyNameReference()
	{
		return TypeReferenceGetAssemblyNameReference();
	}

	public override TypeReference GetElementType()
	{
		return this;
	}

	public override RuntimeStorageKind GetRuntimeStorage(ITypeFactory typeFactory)
	{
		return RuntimeStorageKind.Pointer;
	}

	public override RuntimeFieldLayoutKind GetRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		return RuntimeFieldLayoutKind.Fixed;
	}
}
