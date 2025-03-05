using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public struct VirtualMethodDeclarationData
{
	public readonly IMethodSignature Method;

	public readonly VirtualMethodCallType CallType;

	public readonly bool ReturnsVoid;

	public readonly bool DoCallViaInvoker;

	public readonly ReadOnlyCollection<InvokerParameterData> Parameters;

	public VirtualMethodDeclarationData(IMethodSignature method, ReadOnlyCollection<InvokerParameterData> parameters, bool returnsVoid, VirtualMethodCallType callType, bool doCallViaInvoker)
	{
		Method = method;
		CallType = callType;
		DoCallViaInvoker = doCallViaInvoker;
		ReturnsVoid = returnsVoid;
		Parameters = parameters;
	}
}
