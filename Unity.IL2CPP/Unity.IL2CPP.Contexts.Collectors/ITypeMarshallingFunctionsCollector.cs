using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Collectors;

public interface ITypeMarshallingFunctionsCollector
{
	void Add(SourceWritingContext context, TypeDefinition type);
}
