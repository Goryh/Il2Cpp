using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Collectors;

public interface ISharedMethodCollector
{
	void AddSharedMethod(MethodReference sharedMethod, MethodReference actualMethod);
}
