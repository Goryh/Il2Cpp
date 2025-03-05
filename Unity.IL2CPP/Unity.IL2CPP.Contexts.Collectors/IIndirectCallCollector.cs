using System.Collections.Generic;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Collectors;

public interface IIndirectCallCollector
{
	void Add(SourceWritingContext context, MethodReference method, IndirectCallUsage callUsage, bool skipFirstArg = false);

	void Add(SourceWritingContext context, TypeReference returnType, IReadOnlyList<TypeReference> parameterTypes, IndirectCallUsage callUsage);

	void AddRange(SourceWritingContext context, IEnumerable<MethodReference> methods, IndirectCallUsage callUsage);
}
