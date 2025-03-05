using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Metadata.RuntimeTypes;

public class Il2CppPtrRuntimeType : Il2CppRuntimeTypeBase<PointerType>
{
	public readonly IIl2CppRuntimeType ElementType;

	public Il2CppPtrRuntimeType(PointerType type, int attrs, IIl2CppRuntimeType elementType)
		: base(type, attrs)
	{
		ElementType = elementType;
	}
}
