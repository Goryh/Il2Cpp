using System.Collections.Generic;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationSteps;

internal static class AddMethodsToDefinitionsOfInterestStep
{
	internal static void Run(HashSet<IMemberDefinition> definitionsOfInterest)
	{
		IMemberDefinition[] definitionsOfInterestCopy = new IMemberDefinition[definitionsOfInterest.Count];
		definitionsOfInterest.CopyTo(definitionsOfInterestCopy);
		IMemberDefinition[] array = definitionsOfInterestCopy;
		for (int i = 0; i < array.Length; i++)
		{
			if (!(array[i] is TypeDefinition type))
			{
				continue;
			}
			foreach (MethodDefinition method in type.Methods)
			{
				definitionsOfInterest.Add(method);
			}
		}
	}
}
