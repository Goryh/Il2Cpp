using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Collectors;

public interface ICCWMarshallingFunctionCollector
{
	void Add(PrimaryCollectionContext context, TypeReference type);
}
