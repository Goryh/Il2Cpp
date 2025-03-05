namespace Unity.IL2CPP.AssemblyConversion.Steps.Results;

public class GenericLimitsResults
{
	public readonly int MaximumRecursiveGenericDepth;

	public readonly int VirtualMethodIterations;

	public GenericLimitsResults(int maximumRecursiveGenericDepth, int virtualMethodIterations)
	{
		MaximumRecursiveGenericDepth = maximumRecursiveGenericDepth;
		VirtualMethodIterations = virtualMethodIterations;
	}
}
