using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public class MethodMetadataAccess : IMethodMetadataAccess
{
	private readonly MethodReference _unresolvedMethodToCall;

	private readonly string _hiddenMethodInfo;

	private readonly bool _hasCustomHiddenMethodInfo;

	public IRuntimeMetadataAccess RuntimeMetadataAccess { get; }

	public bool IsConstrainedCall => false;

	public MethodMetadataAccess(IRuntimeMetadataAccess runtimeMetadataAccess, MethodReference unresolvedMethodToCall)
		: this(runtimeMetadataAccess, unresolvedMethodToCall, null, hasCustomHiddenMethodInfo: false)
	{
	}

	private MethodMetadataAccess(IRuntimeMetadataAccess runtimeMetadataAccess, MethodReference unresolvedMethodToCall, string hiddenMethodInfo, bool hasCustomHiddenMethodInfo)
	{
		RuntimeMetadataAccess = runtimeMetadataAccess;
		_unresolvedMethodToCall = unresolvedMethodToCall;
		_hiddenMethodInfo = hiddenMethodInfo;
		_hasCustomHiddenMethodInfo = hasCustomHiddenMethodInfo;
	}

	public string Method(MethodReference resolvedMethodToCall)
	{
		return RuntimeMetadataAccess.Method(resolvedMethodToCall);
	}

	public string MethodInfo()
	{
		return RuntimeMetadataAccess.MethodInfo(_unresolvedMethodToCall);
	}

	public string HiddenMethodInfo()
	{
		if (!_hasCustomHiddenMethodInfo)
		{
			return RuntimeMetadataAccess.HiddenMethodInfo(_unresolvedMethodToCall);
		}
		return _hiddenMethodInfo;
	}

	public string TypeInfoForDeclaringType()
	{
		return RuntimeMetadataAccess.TypeInfoFor(_unresolvedMethodToCall.DeclaringType);
	}

	public bool DoCallViaInvoker(MethodReference resolvedMethodToCall, MethodCallType callType)
	{
		return false;
	}

	public IMethodMetadataAccess OverrideHiddenMethodInfo(string newHiddenMethodInfo)
	{
		return new MethodMetadataAccess(RuntimeMetadataAccess, _unresolvedMethodToCall, newHiddenMethodInfo, hasCustomHiddenMethodInfo: true);
	}

	public IMethodMetadataAccess ForAdjustorThunk()
	{
		return this;
	}
}
