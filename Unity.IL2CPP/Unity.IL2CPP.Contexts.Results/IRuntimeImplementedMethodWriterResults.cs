using System.Collections.Generic;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.GenericSharing;

namespace Unity.IL2CPP.Contexts.Results;

public interface IRuntimeImplementedMethodWriterResults
{
	bool TryGetWriter(MethodDefinition method, out WriteRuntimeImplementedMethodBodyDelegate value);

	bool TryGetGenericSharingDataFor(PrimaryCollectionContext context, MethodDefinition method, out IEnumerable<RuntimeGenericData> value);
}
