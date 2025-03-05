using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.Api.Output.Analytics;
using Unity.IL2CPP.Contexts.Results;

namespace Unity.IL2CPP.AssemblyConversion;

public class ConversionResults
{
	public readonly ReadOnlyCollection<NPath> ConvertedAssemblies;

	public readonly IStatsResults Stats;

	public readonly ReadOnlyCollection<NPath> MatchedAssemblyMethodSourceFiles;

	public readonly ReadOnlyCollection<string> LoggedMessages;

	public readonly IFileResults FilesWritten;

	public readonly Il2CppDataTable AnalyticsTable;

	public ConversionResults(ReadOnlyCollection<NPath> convertedAssemblies, IStatsResults statsResults, ReadOnlyCollection<NPath> matchedAssemblyMethodSourceFiles, ReadOnlyCollection<string> loggedMessages, IFileResults filesWritten, Il2CppDataTable analyticsTable)
	{
		ConvertedAssemblies = convertedAssemblies;
		Stats = statsResults;
		MatchedAssemblyMethodSourceFiles = matchedAssemblyMethodSourceFiles;
		LoggedMessages = loggedMessages;
		FilesWritten = filesWritten;
		AnalyticsTable = analyticsTable;
	}
}
