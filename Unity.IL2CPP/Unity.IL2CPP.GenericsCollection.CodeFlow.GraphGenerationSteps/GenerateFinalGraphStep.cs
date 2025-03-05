using System.Collections.Generic;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationData;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationSteps;

internal static class GenerateFinalGraphStep
{
	public static GenericCodeFlowGraph Run(ref GraphGenerationContext context, Dictionary<TypeDefinition, int> typeIndices, Dictionary<MethodDefinition, int> methodIndices)
	{
		List<TypeDependency> typeDependencies = new List<TypeDependency>();
		List<MethodDependency> methodDependencies = new List<MethodDependency>();
		Node<TypeDefinition>[] typeNodes = new Node<TypeDefinition>[typeIndices.Count];
		Node<MethodDefinition>[] methodNodes = new Node<MethodDefinition>[methodIndices.Count];
		CollectNeededNodes(ref context, context.TypeNodes, typeIndices, methodIndices, typeNodes, typeDependencies, methodDependencies);
		CollectNeededNodes(ref context, context.MethodNodes, typeIndices, methodIndices, methodNodes, typeDependencies, methodDependencies);
		return new GenericCodeFlowGraph(context.Assemblies, methodNodes, typeNodes, methodDependencies, typeDependencies, typeIndices);
	}

	private static void CollectNeededNodes<T>(ref GraphGenerationContext context, List<StagingNode<T>> inNodes, Dictionary<TypeDefinition, int> typeIndices, Dictionary<MethodDefinition, int> methodIndices, Node<T>[] outNodes, List<TypeDependency> outTypeDependencies, List<MethodDependency> outMethodDependencies)
	{
		HashSet<IMemberDefinition> definitionsOfInterest = context.DefinitionsOfInterest;
		List<StagingDependency<TypeReferenceDefinitionPair>> inTypeDependencies = context.TypeDependencies;
		List<StagingDependency<MethodReferenceDefinitionPair>> inMethodDependencies = context.MethodDependencies;
		int nodeCount = inNodes.Count;
		int nodeIndex = 0;
		for (int i = 0; i < nodeCount; i++)
		{
			StagingNode<T> node = inNodes[i];
			if (node.IsNeeded)
			{
				int typeDependenciesStartIndex = outTypeDependencies.Count;
				int methodDependenciesStartIndex = outMethodDependencies.Count;
				CollectTypeDependencies(definitionsOfInterest, inTypeDependencies, node.TypeDependenciesStartIndex, node.TypeDependenciesEndIndex, typeIndices, outTypeDependencies);
				CollectMethodDependencies(definitionsOfInterest, inMethodDependencies, node.MethodDependenciesStartIndex, node.MethodDependenciesEndIndex, methodIndices, outMethodDependencies);
				int typeDependenciesEndIndex = outTypeDependencies.Count;
				int methodDependenciesEndIndex = outMethodDependencies.Count;
				outNodes[nodeIndex++] = new Node<T>(node.Item, methodDependenciesStartIndex, methodDependenciesEndIndex, typeDependenciesStartIndex, typeDependenciesEndIndex);
			}
		}
	}

	private static void CollectTypeDependencies(HashSet<IMemberDefinition> definitionsOfInterest, List<StagingDependency<TypeReferenceDefinitionPair>> inDependencies, int startIndex, int endIndex, Dictionary<TypeDefinition, int> typeIndices, List<TypeDependency> outDependencies)
	{
		_ = inDependencies.Count;
		for (int i = startIndex; i < endIndex; i++)
		{
			StagingDependency<TypeReferenceDefinitionPair> dependency = inDependencies[i];
			if (dependency.IsNeeded)
			{
				TypeReferenceDefinitionPair typeDependency = dependency.Dependency;
				TypeDependencyKind kind = typeDependency.Kind;
				if (definitionsOfInterest.Contains(typeDependency.Definition))
				{
					kind |= TypeDependencyKind.IsOfInterest;
				}
				outDependencies.Add(new TypeDependency(typeDependency.Reference, GetDefinitionIndex(typeIndices, typeDependency.Definition), kind));
			}
		}
	}

	private static void CollectMethodDependencies(HashSet<IMemberDefinition> definitionsOfInterest, List<StagingDependency<MethodReferenceDefinitionPair>> inDependencies, int startIndex, int endIndex, Dictionary<MethodDefinition, int> methodIndices, List<MethodDependency> outDependencies)
	{
		_ = inDependencies.Count;
		for (int i = startIndex; i < endIndex; i++)
		{
			StagingDependency<MethodReferenceDefinitionPair> dependency = inDependencies[i];
			if (dependency.IsNeeded)
			{
				MethodReferenceDefinitionPair methodDependency = dependency.Dependency;
				bool isOfInterest = definitionsOfInterest.Contains(methodDependency.Definition);
				outDependencies.Add(new MethodDependency(methodDependency.Reference, GetDefinitionIndex(methodIndices, methodDependency.Definition), isOfInterest));
			}
		}
	}

	private static int GetDefinitionIndex<T>(Dictionary<T, int> indices, T definition)
	{
		if (definition == null)
		{
			return -1;
		}
		if (indices.TryGetValue(definition, out var index))
		{
			return index;
		}
		return -1;
	}
}
