using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Collectors;

public interface IWindowsRuntimeTypeWithNameCollector
{
	void AddWindowsRuntimeTypeWithName(PrimaryCollectionContext context, TypeReference type, string typeName);
}
