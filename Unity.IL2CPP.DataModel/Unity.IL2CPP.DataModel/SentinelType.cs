using System;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public class SentinelType : TypeSpecification
{
	public override bool IsValueType
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override string FullName => Name;

	public override bool IsSentinel => true;

	public override MetadataType MetadataType => MetadataType.Sentinel;

	protected override bool IsFullNameBuilt => true;

	internal SentinelType(TypeReference elementType, TypeContext context)
		: base(elementType, context)
	{
		InitializeName("[SENTINEL]");
	}

	public override RuntimeStorageKind GetRuntimeStorage(ITypeFactory typeFactory)
	{
		throw new NotSupportedException();
	}
}
