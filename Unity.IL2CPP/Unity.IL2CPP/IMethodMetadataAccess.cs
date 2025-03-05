using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public interface IMethodMetadataAccess
{
	IRuntimeMetadataAccess RuntimeMetadataAccess { get; }

	bool IsConstrainedCall { get; }

	string Method(MethodReference resolvedMethodToCall);

	string MethodInfo();

	string HiddenMethodInfo();

	string TypeInfoForDeclaringType();

	bool DoCallViaInvoker(MethodReference resolvedMethodToCall, MethodCallType callType);

	IMethodMetadataAccess OverrideHiddenMethodInfo(string newHiddenMethodInfo);

	IMethodMetadataAccess ForAdjustorThunk();
}
