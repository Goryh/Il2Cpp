using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Services;

public interface IContextScopeService
{
	string UniqueIdentifier { get; }

	bool IncludeTypeDefinitionInContext(TypeReference type);
}
