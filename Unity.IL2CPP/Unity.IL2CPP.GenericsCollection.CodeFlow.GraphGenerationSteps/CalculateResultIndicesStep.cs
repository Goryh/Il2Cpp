using System.Collections.Generic;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationSteps;

internal static class CalculateResultIndicesStep
{
	public static Dictionary<T, int> Run<T>(List<StagingNode<T>> stagingNodes)
	{
		Dictionary<T, int> indices = new Dictionary<T, int>();
		int currentIndex = 0;
		int count = stagingNodes.Count;
		for (int i = 0; i < count; i++)
		{
			StagingNode<T> node = stagingNodes[i];
			if (node.IsNeeded)
			{
				indices[node.Item] = currentIndex++;
			}
		}
		return indices;
	}
}
