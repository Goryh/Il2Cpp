namespace Unity.IL2CPP.DataModel;

public sealed class SafeArrayMarshalInfo : MarshalInfo
{
	public VariantType ElementType { get; }

	public SafeArrayMarshalInfo(VariantType variantType = VariantType.None)
		: base(NativeType.SafeArray)
	{
		ElementType = variantType;
	}
}
