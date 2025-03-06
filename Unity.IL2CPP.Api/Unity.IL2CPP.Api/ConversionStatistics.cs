using Unity.Api.Attributes;

namespace Unity.IL2CPP.Api;

[ContainsOptions]
public sealed class ConversionStatistics
{
	[HelpDetails("The directory where statistics information will be written", "path")]
	[IsPath(PathKind.ExplicitlyHandledInBuildCode)]
	public string StatsOutputDir;
}
