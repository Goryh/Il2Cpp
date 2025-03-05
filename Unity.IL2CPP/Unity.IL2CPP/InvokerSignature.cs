using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public struct InvokerSignature
{
	public readonly bool HasThis;

	public readonly TypeReference[] ReducedParameterTypes;

	public static InvokerSignature Create(ReadOnlyContext context, MethodReference method)
	{
		method = method.GetSharedMethodIfSharableOtherwiseSelf(context);
		return new InvokerSignature(method.HasThis, TypeCollapser.CollapseSignature(context, method));
	}

	private InvokerSignature(bool hasThis, TypeReference[] reducedParameterTypes)
	{
		HasThis = hasThis;
		ReducedParameterTypes = reducedParameterTypes;
	}
}
