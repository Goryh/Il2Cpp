using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericSharing;

public class RuntimeGenericConstrainedCallData : RuntimeGenericData
{
	public readonly TypeReference ConstrainedType;

	public readonly MethodReference ConstrainedMethod;

	public RuntimeGenericConstrainedCallData(RuntimeGenericContextInfo infoType, TypeReference constrainedType, MethodReference constrainedMethod)
		: base(infoType)
	{
		ConstrainedType = constrainedType;
		ConstrainedMethod = constrainedMethod;
	}
}
