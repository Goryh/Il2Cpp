using System.Collections.Generic;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationData;
using Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationSteps;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow;

public class GenericCodeFlowGraphGenerator
{
	public static GenericCodeFlowGraph Generate(ReadOnlyContext context, ref InputData inputData, IEnumerable<AssemblyDefinition> assemblies)
	{
		ITinyProfilerService tinyProfiler = context.Global.Services.TinyProfiler;
		using (tinyProfiler.Section("GenericCodeFlowGraphGenerator.Generate"))
		{
			using (tinyProfiler.Section("AddMethodsToDefinitionsOfInterest"))
			{
				AddMethodsToDefinitionsOfInterestStep.Run(inputData.DefinitionsOfInterest);
			}
			GraphGenerationContext graphGenerationContext = new GraphGenerationContext(ref inputData, assemblies, context.Global.Services.TypeFactory);
			using (tinyProfiler.Section("GenerateFullGraph"))
			{
				FullGraphGenerationStep.Run(ref graphGenerationContext);
			}
			Dictionary<IMemberDefinition, List<int>> dependencyLookupMap = new Dictionary<IMemberDefinition, List<int>>();
			using (tinyProfiler.Section("BuildTypeDependencyLookupMap"))
			{
				BuildDependencyLookupMapStep.Run(dependencyLookupMap, graphGenerationContext.TypeDependencies);
			}
			using (tinyProfiler.Section("BuildMethodDependencyLookupMap"))
			{
				BuildDependencyLookupMapStep.Run(dependencyLookupMap, graphGenerationContext.MethodDependencies);
			}
			using (tinyProfiler.Section("MarkNeededNodes"))
			{
				MarkNeededNodesStep.Run(ref graphGenerationContext, dependencyLookupMap);
			}
			Dictionary<TypeDefinition, int> typeIndices;
			using (tinyProfiler.Section("CalculateTypeIndices"))
			{
				typeIndices = CalculateResultIndicesStep.Run(graphGenerationContext.TypeNodes);
			}
			Dictionary<MethodDefinition, int> methodIndices;
			using (tinyProfiler.Section("CalculateMethodIndices"))
			{
				methodIndices = CalculateResultIndicesStep.Run(graphGenerationContext.MethodNodes);
			}
			using (tinyProfiler.Section("GenerateFinalGraph"))
			{
				return GenerateFinalGraphStep.Run(ref graphGenerationContext, typeIndices, methodIndices);
			}
		}
	}
}
