using System.Threading;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public class RequiredModifierType : TypeSpecification
{
	private string _name;

	private string _fullName;

	public TypeReference ModifierType { get; }

	public override bool IsValueType => base.ElementType.IsValueType;

	public override bool IsRequiredModifier => true;

	public override MetadataType MetadataType => MetadataType.RequiredModifier;

	public override string Name
	{
		get
		{
			if (_name == null)
			{
				Interlocked.CompareExchange(ref _name, LazyNameHelpers.GetName(this), null);
			}
			return _name;
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

	internal RequiredModifierType(TypeReference modifierType, TypeReference elementType, TypeContext context)
		: base(elementType, context)
	{
		ModifierType = modifierType;
	}

	public override RuntimeStorageKind GetRuntimeStorage(ITypeFactory typeFactory)
	{
		return base.ElementType.GetRuntimeStorage(typeFactory);
	}

	public override RuntimeFieldLayoutKind GetRuntimeFieldLayout(ITypeFactory typeFactory)
	{
		return base.ElementType.GetRuntimeFieldLayout(typeFactory);
	}
}
