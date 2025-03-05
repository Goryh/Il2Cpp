using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Collectors;

public interface IGenericMethodCollector
{
	void Add(SourceWritingContext context, MethodReference method);

	void Add(PrimaryCollectionContext context, MethodReference method);
}
