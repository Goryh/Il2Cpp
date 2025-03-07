using System.Collections.ObjectModel;

namespace Unity.IL2CPP.DataModel;

public interface IGenericInstance : IMetadataTokenProvider
{
	bool HasGenericArguments { get; }

	ReadOnlyCollection<TypeReference> GenericArguments { get; }

	int RecursiveGenericDepth { get; }
}
