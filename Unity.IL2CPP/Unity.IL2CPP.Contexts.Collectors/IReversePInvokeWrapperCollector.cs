using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Collectors;

public interface IReversePInvokeWrapperCollector
{
	void AddReversePInvokeWrapper(MethodReference method);
}
