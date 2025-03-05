using System.Collections.Generic;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Collectors;

public interface IInteropGuidCollector
{
	void Add(SourceWritingContext context, TypeReference type);

	void Add(SourceWritingContext context, IEnumerable<TypeReference> types);
}
