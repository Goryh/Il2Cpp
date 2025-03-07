using System.Collections.ObjectModel;
using System.Threading;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public abstract class FieldReference : MemberReference
{
	private bool _propertiesInitialized;

	private bool _isVolatile;

	private string _fullName;

	private string _cppName;

	public abstract bool IsThreadStatic { get; }

	public abstract bool IsNormalStatic { get; }

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

	public bool IsVolatile
	{
		get
		{
			if (!_propertiesInitialized)
			{
				ThrowDataNotInitialized("IsVolatile");
			}
			return _isVolatile;
		}
	}

	public bool HasCustomAttributes => CustomAttributes.Count > 0;

	public abstract ReadOnlyCollection<CustomAttribute> CustomAttributes { get; }

	public abstract TypeReference FieldType { get; }

	public string CppName
	{
		get
		{
			if (_cppName == null)
			{
				Interlocked.CompareExchange(ref _cppName, CppNamePopulator.GetFieldRefCppName(this), null);
			}
			return _cppName;
		}
	}

	public abstract FieldAttributes Attributes { get; }

	public bool IsCompilerControlled => (Attributes & FieldAttributes.FieldAccessMask) == 0;

	public bool IsSpecialName => Attributes.HasFlag(FieldAttributes.SpecialName);

	public bool IsRuntimeSpecialName => Attributes.HasFlag(FieldAttributes.RTSpecialName);

	public bool HasDefault => Attributes.HasFlag(FieldAttributes.HasDefault);

	public bool IsLiteral => Attributes.HasFlag(FieldAttributes.Literal);

	public bool IsStatic => Attributes.HasFlag(FieldAttributes.Static);

	public bool IsPInvokeImpl => Attributes.HasFlag(FieldAttributes.PInvokeImpl);

	public abstract int FieldIndex { get; }

	public override bool ContainsGenericParameter
	{
		get
		{
			if (!FieldType.ContainsGenericParameter)
			{
				return base.ContainsGenericParameter;
			}
			return true;
		}
	}

	public abstract FieldDefinition FieldDef { get; }

	protected override bool IsFullNameBuilt => _fullName != null;

	protected FieldReference(TypeReference declaringType, MetadataToken metadataToken)
		: base(declaringType, metadataToken)
	{
	}

	public abstract TypeReference ResolvedFieldType(ITypeFactory typeFactory);

	internal void InitializeProperties(bool isVolatile)
	{
		_propertiesInitialized = true;
		_isVolatile = isVolatile;
	}
}
