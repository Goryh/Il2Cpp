using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata;

public class RGCTXEntry
{
	public readonly RGCTXType Type;

	public readonly MethodReference MethodReference;

	public readonly IIl2CppRuntimeType RuntimeType;

	public RGCTXEntry(RGCTXType type, IIl2CppRuntimeType runtimeType)
		: this(type, runtimeType, null)
	{
	}

	public RGCTXEntry(RGCTXType type, MethodReference methodReference)
		: this(type, null, methodReference)
	{
	}

	public RGCTXEntry(RGCTXType type, IIl2CppRuntimeType runtimeType, MethodReference methodReference)
	{
		Type = type;
		MethodReference = methodReference;
		RuntimeType = runtimeType;
	}
}
