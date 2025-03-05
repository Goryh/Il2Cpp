using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Attributes;

public readonly struct CustomAttributeArgumentData
{
	public readonly IIl2CppRuntimeType Type;

	public readonly AttributeDataItem Data;

	private CustomAttributeArgumentData(IIl2CppRuntimeType type, AttributeDataItem data)
	{
		Type = type;
		Data = data;
	}

	public static CustomAttributeArgumentData Create(GlobalPrimaryCollectionContext context, CustomAttributeArgument arg, in AttributeDataItem data)
	{
		IIl2CppRuntimeType type = ((!(arg.Value is CustomAttributeArgument innerArg)) ? context.Collectors.Types.Add(arg.Type) : context.Collectors.Types.Add(innerArg.Type));
		return new CustomAttributeArgumentData(type, data);
	}
}
