using System;
using System.Collections.Generic;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationData;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationSteps;

internal static class MarkNeededNodesStep
{
	public static void Run(ref GraphGenerationContext context, Dictionary<IMemberDefinition, List<int>> dependencyLookupMap)
	{
		List<StagingNode<TypeDefinition>> typeNodes = context.TypeNodes;
		List<StagingNode<MethodDefinition>> methodNodes = context.MethodNodes;
		List<StagingDependency<TypeReferenceDefinitionPair>> typeDependencies = context.TypeDependencies;
		List<StagingDependency<MethodReferenceDefinitionPair>> methodDependencies = context.MethodDependencies;
		List<int> methodNodesToProcess = new List<int>(typeNodes.Count);
		List<int> typeNodesToProcess = new List<int>(methodNodes.Count);
		AddNeededNodes(context.DefinitionsOfInterest, typeDependencies, typeNodesToProcess, methodNodesToProcess);
		AddNeededNodes(context.DefinitionsOfInterest, methodDependencies, typeNodesToProcess, methodNodesToProcess);
		while (methodNodesToProcess.Count > 0 || typeNodesToProcess.Count > 0)
		{
			ProcessNeededNodes(typeNodes, typeNodesToProcess, typeNodesToProcess, methodNodesToProcess, dependencyLookupMap, typeDependencies);
			ProcessNeededNodes(methodNodes, methodNodesToProcess, typeNodesToProcess, methodNodesToProcess, dependencyLookupMap, methodDependencies);
		}
	}

	private static void AddNeededNodes<T>(HashSet<IMemberDefinition> definitionsOfInterest, List<StagingDependency<T>> dependencies, List<int> typeNodesToProcess, List<int> methodNodesToProcess) where T : IHasDefinition, IEquatable<T>
	{
		int typeDependencyCount = dependencies.Count;
		for (int i = 0; i < typeDependencyCount; i++)
		{
			StagingDependency<T> node = dependencies[i];
			IMemberDefinition definition = node.Dependency.GetDefinition();
			if (definition == null || definitionsOfInterest.Contains(definition))
			{
				node.IsNeeded = true;
				dependencies[i] = node;
				AddReferrerNode(typeNodesToProcess, methodNodesToProcess, node.ReferrerIndex);
			}
		}
	}

	private static void ProcessNeededNodes<T, TPair>(List<StagingNode<T>> nodes, List<int> nodesToProcess, List<int> typeNodesToProcess, List<int> methodNodesToProcess, Dictionary<IMemberDefinition, List<int>> dependencyLookupMap, List<StagingDependency<TPair>> dependencies) where T : IMemberDefinition where TPair : IEquatable<TPair>
	{
		int nodeCount = nodesToProcess.Count;
		if (nodeCount == 0)
		{
			return;
		}
		int nodeIndex = nodesToProcess[nodeCount - 1];
		StagingNode<T> node = nodes[nodeIndex];
		nodesToProcess.RemoveAt(nodeCount - 1);
		if (node.IsNeeded)
		{
			return;
		}
		node.IsNeeded = true;
		nodes[nodeIndex] = node;
		if (dependencyLookupMap.TryGetValue(node.Item, out var indices))
		{
			int indexCount = indices.Count;
			for (int i = 0; i < indexCount; i++)
			{
				int index = indices[i];
				StagingDependency<TPair> dependency = dependencies[index];
				dependency.IsNeeded = true;
				dependencies[index] = dependency;
				AddReferrerNode(typeNodesToProcess, methodNodesToProcess, dependency.ReferrerIndex);
			}
		}
	}

	private static void AddReferrerNode(List<int> typeNodesToProcess, List<int> methodNodesToProcess, int referrerIndex)
	{
		if ((referrerIndex & 0x80000000u) != 0L)
		{
			typeNodesToProcess.Add(referrerIndex & 0x7FFFFFFF);
		}
		else
		{
			methodNodesToProcess.Add(referrerIndex);
		}
	}
}
