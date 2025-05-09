using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericSharing;

public class RuntimeGenericTypeData : RuntimeGenericData
{
	public readonly TypeReference GenericType;

	public RuntimeGenericTypeData(RuntimeGenericContextInfo infoType, TypeReference genericType)
		: base(infoType)
	{
		GenericType = genericType;
	}
}
