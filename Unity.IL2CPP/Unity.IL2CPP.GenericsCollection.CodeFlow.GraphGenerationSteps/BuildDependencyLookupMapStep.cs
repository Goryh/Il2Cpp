using System;
using System.Collections.Generic;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationSteps;

internal static class BuildDependencyLookupMapStep
{
	public static void Run<TPair>(Dictionary<IMemberDefinition, List<int>> lookupMap, List<StagingDependency<TPair>> dependencies) where TPair : IHasDefinition, IEquatable<TPair>
	{
		int dependencyCount = dependencies.Count;
		for (int i = 0; i < dependencyCount; i++)
		{
			IMemberDefinition definition = dependencies[i].Dependency.GetDefinition();
			if (definition != null)
			{
				if (!lookupMap.TryGetValue(definition, out var indices))
				{
					lookupMap.Add(definition, indices = new List<int>());
				}
				indices.Add(i);
			}
		}
	}
}
