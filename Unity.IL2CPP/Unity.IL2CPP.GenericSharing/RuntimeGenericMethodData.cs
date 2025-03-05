using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericSharing;

public class RuntimeGenericMethodData : RuntimeGenericData
{
	public readonly MethodReference GenericMethod;

	public RuntimeGenericMethodData(RuntimeGenericContextInfo infoType, MethodReference genericMethod)
		: base(infoType)
	{
		GenericMethod = genericMethod;
	}
}
