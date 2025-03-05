namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;

public class AssemblyAnalyticsData
{
	public int EagerStaticConstructorAttributeCount { get; init; }

	public int SetOptionAttributeCount { get; init; }

	public int GenerateIntoOwnCppFileAttributeCount { get; init; }

	public int IgnoredByDeepProfilerAttributeCount { get; init; }
}
