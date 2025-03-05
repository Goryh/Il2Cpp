using System;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Services;

public interface IGuidProvider
{
	Guid GuidFor(ReadOnlyContext context, TypeReference type);
}
