using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.Common.Profiles;

namespace Unity.IL2CPP.AssemblyConversion;

public class AssemblyConversionInputData
{
	public readonly NPath OutputDir;

	public readonly NPath DataFolder;

	public readonly NPath SymbolsFolder;

	public readonly NPath MetadataFolder;

	public readonly NPath ExecutableAssembliesFolder;

	public readonly NPath StatsOutputDir;

	public readonly string EntryAssemblyName;

	public readonly NPath[] ExtraTypesFiles;

	public readonly RuntimeProfile Profile;

	public readonly ReadOnlyCollection<NPath> Assemblies;

	public readonly int UserSuppliedMaximumRecursiveGenericDepth;

	public readonly int UserSuppliedGenericVirtualMethodIterations;

	public readonly string AssemblyMethod;

	public readonly int JobCount;

	public readonly string[] DebugAssemblyName;

	public readonly NPath[] AdditionalCpp;

	public readonly NPath DistributionDirectory;

	public AssemblyConversionInputData(NPath outputDir, NPath dataFolder, NPath symbolsFolder, NPath metadataFolder, NPath executableAssembliesFolder, NPath statsOutputDir, string entryAssemblyName, NPath[] extraTypesFiles, RuntimeProfile profile, ReadOnlyCollection<NPath> assemblies, int maximumRecursiveGenericDepth, int genericVirtualMethodIterations, string assemblyMethod, int jobCount, string[] debugAssemblyName, NPath[] additionalCpp, NPath distributionDirectory)
	{
		OutputDir = outputDir;
		DataFolder = dataFolder;
		SymbolsFolder = symbolsFolder;
		MetadataFolder = metadataFolder;
		ExecutableAssembliesFolder = executableAssembliesFolder;
		StatsOutputDir = statsOutputDir;
		EntryAssemblyName = entryAssemblyName;
		ExtraTypesFiles = extraTypesFiles;
		Profile = profile;
		Assemblies = assemblies;
		UserSuppliedMaximumRecursiveGenericDepth = maximumRecursiveGenericDepth;
		UserSuppliedGenericVirtualMethodIterations = genericVirtualMethodIterations;
		AssemblyMethod = assemblyMethod;
		JobCount = jobCount;
		DebugAssemblyName = debugAssemblyName;
		AdditionalCpp = additionalCpp;
		DistributionDirectory = distributionDirectory;
	}
}
