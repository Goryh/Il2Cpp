using System.Collections.Generic;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow;

public struct GenericCodeFlowGraph
{
	private struct CollectionContext
	{
		public readonly ICodeFlowCollection Generics;

		public readonly HashSet<TypeReference> VisitedTypes;

		public readonly HashSet<MethodReference> VisitedMethods;

		public readonly List<TypeReference> TypesForCCWs;

		public readonly List<GenericInstanceType> FoundGenericTypes;

		public CollectionContext(ICodeFlowCollection generics)
		{
			Generics = generics;
			VisitedMethods = new HashSet<MethodReference>();
			VisitedTypes = new HashSet<TypeReference>();
			TypesForCCWs = new List<TypeReference>();
			FoundGenericTypes = new List<GenericInstanceType>();
		}
	}

	private readonly IEnumerable<AssemblyDefinition> AllAssemblies;

	private readonly Node<MethodDefinition>[] MethodNodes;

	private readonly Node<TypeDefinition>[] TypeNodes;

	private readonly MethodDependency[] MethodDependencies;

	private readonly TypeDependency[] TypeDependencies;

	private readonly Dictionary<TypeDefinition, int> TypeIndices;

	internal GenericCodeFlowGraph(IEnumerable<AssemblyDefinition> allAssemblies, Node<MethodDefinition>[] methodNodes, Node<TypeDefinition>[] typeNodes, List<MethodDependency> methodDependencies, List<TypeDependency> typeDependencies, Dictionary<TypeDefinition, int> typeIndices)
	{
		AllAssemblies = allAssemblies;
		MethodNodes = methodNodes;
		TypeNodes = typeNodes;
		MethodDependencies = new MethodDependency[methodDependencies.Count];
		methodDependencies.CopyTo(MethodDependencies);
		TypeDependencies = new TypeDependency[typeDependencies.Count];
		typeDependencies.CopyTo(TypeDependencies);
		TypeIndices = typeIndices;
	}

	public void CollectGenerics(PrimaryCollectionContext context, ICodeFlowCollection generics)
	{
		CollectionContext collectionContext = new CollectionContext(generics);
		ITinyProfilerService tinyProfiler = context.Global.Services.TinyProfiler;
		using (tinyProfiler.Section("GenericCodeFlowGraph.CollectGenerics"))
		{
			CollectGenerics(context, ref collectionContext);
		}
		using (tinyProfiler.Section("GenericCodeFlowGraph.CollectCCWs"))
		{
			CollectCCWs(context, ref collectionContext);
		}
		using (tinyProfiler.Section("GenericCodeFlowGraph.DispatchToGenericContextAwareVisitor"))
		{
			DispatchToGenericContextAwareVisitor(ref collectionContext);
		}
	}

	private void CollectCCWs(ReadOnlyContext context, ref CollectionContext collectionContext)
	{
		foreach (AssemblyDefinition allAssembly in AllAssemblies)
		{
			foreach (TypeDefinition type in allAssembly.GetAllTypes())
			{
				if (!type.HasGenericParameters)
				{
					CollectCCWsForType(context, ref collectionContext, type);
				}
			}
		}
		for (int i = 0; i < collectionContext.TypesForCCWs.Count; i++)
		{
			CollectCCWsForType(context, ref collectionContext, collectionContext.TypesForCCWs[i]);
		}
	}

	private void CollectCCWsForType(ReadOnlyContext context, ref CollectionContext collectionContext, TypeReference type)
	{
		if (!type.NeedsComCallableWrapper(context))
		{
			return;
		}
		foreach (TypeReference iface in type.GetInterfacesImplementedByComCallableWrapper(context))
		{
			if (iface is GenericInstanceType && TypeIndices.TryGetValue(iface.Resolve(), out var nodeIndex) && nodeIndex != -1)
			{
				CollectGenericsRecursive(context, ref collectionContext, iface, TypeNodes[nodeIndex]);
			}
		}
	}

	private void CollectGenerics(ReadOnlyContext context, ref CollectionContext collectionContext)
	{
		int typeNodeCount = TypeNodes.Length;
		for (int i = 0; i < typeNodeCount; i++)
		{
			Node<TypeDefinition> node = TypeNodes[i];
			TypeDefinition type = node.Item;
			if (!type.HasGenericParameters)
			{
				CollectGenericsRecursive(context, ref collectionContext, type, node);
			}
		}
		int methodNodeCount = MethodNodes.Length;
		for (int j = 0; j < methodNodeCount; j++)
		{
			Node<MethodDefinition> node2 = MethodNodes[j];
			MethodDefinition method = node2.Item;
			if (!method.HasGenericParameters && !method.DeclaringType.HasGenericParameters)
			{
				CollectGenericsRecursive(context, ref collectionContext, method, node2);
			}
		}
	}

	private void CollectGenericsRecursive(ReadOnlyContext context, ref CollectionContext collectionContext, TypeReference type, Node<TypeDefinition> node)
	{
		if (!GenericsUtilities.CheckForMaximumRecursion(context, type) && collectionContext.VisitedTypes.Add(type))
		{
			TypeResolver resolver = context.Global.Services.TypeFactory.ResolverFor(type);
			CollectTypeDependencies(context, ref collectionContext, resolver, node.TypeDependenciesStartIndex, node.TypeDependenciesEndIndex);
			CollectMethodDependencies(context, ref collectionContext, resolver, node.MethodDependenciesStartIndex, node.MethodDependenciesEndIndex);
		}
	}

	private void CollectGenericsRecursive(ReadOnlyContext context, ref CollectionContext collectionContext, MethodReference method, Node<MethodDefinition> node)
	{
		if ((!(method is GenericInstanceMethod genericInstanceMethod) || !GenericsUtilities.CheckForMaximumRecursion(context, genericInstanceMethod)) && (!(method.DeclaringType is GenericInstanceType genericInstanceType) || !GenericsUtilities.CheckForMaximumRecursion(context, genericInstanceType)) && collectionContext.VisitedMethods.Add(method))
		{
			TypeResolver resolver = context.Global.Services.TypeFactory.ResolverFor(method.DeclaringType, method);
			CollectTypeDependencies(context, ref collectionContext, resolver, node.TypeDependenciesStartIndex, node.TypeDependenciesEndIndex);
			CollectMethodDependencies(context, ref collectionContext, resolver, node.MethodDependenciesStartIndex, node.MethodDependenciesEndIndex);
		}
	}

	private void CollectTypeDependencies(ReadOnlyContext context, ref CollectionContext collectionContext, TypeResolver resolver, int typeDependenciesStartIndex, int typeDependenciesEndIndex)
	{
		for (int i = typeDependenciesStartIndex; i < typeDependenciesEndIndex; i++)
		{
			TypeDependency dependency = TypeDependencies[i];
			TypeReference resolvedDependency = resolver.Resolve(dependency.Type);
			if (HasFlag(dependency.Kind, TypeDependencyKind.InstantiatedArray))
			{
				if (collectionContext.Generics.AddInstantiatedArray((ArrayType)resolvedDependency))
				{
					collectionContext.TypesForCCWs.Add(resolvedDependency);
				}
			}
			else if (HasFlag(dependency.Kind, TypeDependencyKind.IsOfInterest))
			{
				GenericInstanceType genericInstance = (GenericInstanceType)resolvedDependency;
				if ((HasFlag(dependency.Kind, TypeDependencyKind.InstantiatedGenericInstance | TypeDependencyKind.ImplicitDependency) || (HasFlag(dependency.Kind, TypeDependencyKind.MethodParameterOrReturnType) && genericInstance.IsValueType)) && collectionContext.Generics.AddInstantiatedGeneric(genericInstance))
				{
					collectionContext.TypesForCCWs.Add(resolvedDependency);
				}
				collectionContext.FoundGenericTypes.Add(genericInstance);
			}
			if (dependency.DefinitionIndex != -1)
			{
				CollectGenericsRecursive(context, ref collectionContext, resolvedDependency, TypeNodes[dependency.DefinitionIndex]);
			}
		}
	}

	private void CollectMethodDependencies(ReadOnlyContext context, ref CollectionContext collectionContext, TypeResolver resolver, int methodDependenciesStartIndex, int methodDependenciesEndIndex)
	{
		for (int i = methodDependenciesStartIndex; i < methodDependenciesEndIndex; i++)
		{
			MethodDependency dependency = MethodDependencies[i];
			MethodReference resolvedDependency = resolver.Resolve(dependency.Method);
			if (dependency.IsOfInterest && resolvedDependency.DeclaringType is GenericInstanceType genericDeclaringType)
			{
				collectionContext.FoundGenericTypes.Add(genericDeclaringType);
			}
			if (dependency.DefinitionIndex != -1)
			{
				CollectGenericsRecursive(context, ref collectionContext, resolvedDependency, MethodNodes[dependency.DefinitionIndex]);
			}
		}
	}

	private void DispatchToGenericContextAwareVisitor(ref CollectionContext collectionContext)
	{
		ICodeFlowCollection genericsCollection = collectionContext.Generics;
		foreach (GenericInstanceType type in collectionContext.FoundGenericTypes)
		{
			genericsCollection.AddFoundGenericType(type);
		}
	}

	private static bool HasFlag(TypeDependencyKind value, TypeDependencyKind flag)
	{
		return (value & flag) != 0;
	}
}
