using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Collectors;

public interface IMethodCollector
{
	void Add(MethodReference method);
}
