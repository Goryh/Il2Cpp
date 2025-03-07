using System.Threading;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public class PinnedType : TypeSpecification
{
	private string _fullName;

	public override bool IsValueType => base.ElementType.IsValueType;

	public override bool IsPinned => true;

	public override MetadataType MetadataType => MetadataType.Pinned;

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

	internal PinnedType(TypeReference elementType, TypeContext context)
		: base(elementType, context)
	{
		InitializeName(elementType.Name);
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
