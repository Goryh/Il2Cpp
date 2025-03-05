using System;
using System.Collections.Generic;
using System.IO;
using NiceIO;
using Unity.IL2CPP.Api;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Results;

namespace il2cpp;

public static class StatisticsGenerator
{
	private const double BytesInKilobyte = 1024.0;

	private const string StatisticsLogFileName = "statistics.txt";

	public static NPath DetermineAndSetupOutputDirectory(Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		NPath dir = il2CppCommandLineArguments.ConversionRequest.Statistics.StatsOutputDir.ToNPath();
		if (!dir.Exists())
		{
			dir.CreateDirectory();
		}
		return dir;
	}

	public static void Generate(Il2CppCommandLineArguments il2CppCommandLineArguments, IStatsResults conversionStats)
	{
		NPath statsOutputDirectory = il2CppCommandLineArguments.ConversionRequest.Statistics.StatsOutputDir.ToNPath();
		WriteSingleLog(statsOutputDirectory, conversionStats);
		WriteBasicDataToStdout(statsOutputDirectory);
	}

	private static void WriteSingleLog(NPath statsOutputDirectory, IStatsResults conversionStats)
	{
		using StreamWriter writer = new StreamWriter(statsOutputDirectory.Combine("statistics.txt").ToString());
		WriteStatsLog(writer, conversionStats);
	}

	public static void WriteStatsLog(TextWriter writer, IStatsResults conversionStats)
	{
		writer.WriteLine("----- il2cpp Statistics -----");
		if (conversionStats != null)
		{
			writer.WriteLine("General:");
			writer.WriteLine($"\tProcessor Count: {Environment.ProcessorCount}");
			writer.WriteLine("\tFiles Written: {0}", conversionStats.FilesWritten);
			writer.WriteLine("\tString Literals: {0}", conversionStats.StringLiterals);
			writer.WriteLine("Methods:");
			writer.WriteLine("\tTotal Methods: {0}", conversionStats.Methods);
			writer.WriteLine("\tNon-Generic Methods: {0}", conversionStats.Methods - (conversionStats.GenericTypeMethods + conversionStats.GenericMethods));
			writer.WriteLine("\tGeneric Type Methods: {0}", conversionStats.GenericTypeMethods);
			writer.WriteLine("\tGeneric Methods: {0}", conversionStats.GenericMethods);
			writer.WriteLine("\tShared Methods: {0}", conversionStats.ShareableMethods);
			writer.WriteLine("\tMethods with Tail Calls : {0}", conversionStats.TailCallsEncountered);
			writer.WriteLine("Metadata:");
			writer.WriteLine("\tTotal: {0:N2} kb", (double)conversionStats.MetadataTotal / 1024.0);
			foreach (KeyValuePair<string, long> streamData in conversionStats.MetadataStreams)
			{
				writer.WriteLine("\t{0}: {1:N2} kb", streamData.Key, (double)streamData.Value / 1024.0);
			}
			writer.WriteLine("Codegen:");
			writer.WriteLine("\tNullChecks : {0}", conversionStats.TotalNullChecks);
			writer.WriteLine("Interop:");
			writer.WriteLine($"\tWindows Runtime boxed types : {conversionStats.WindowsRuntimeBoxedTypes}");
			writer.WriteLine($"\tWindows Runtime types with names : {conversionStats.WindowsRuntimeTypesWithNames}");
			writer.WriteLine($"\tNative to managed interface adapters : {conversionStats.NativeToManagedInterfaceAdapters}");
			writer.WriteLine($"\tArray COM callable wrappers : {conversionStats.ArrayComCallableWrappers}");
			writer.WriteLine($"\tCOM callable wrappers : {conversionStats.ComCallableWrappers}");
			writer.WriteLine($"\tCOM callable wrapper methods that were implemented : {conversionStats.ImplementedComCallableWrapperMethods}");
			writer.WriteLine($"\tCOM callable wrapper methods that were stripped : {conversionStats.StrippedComCallableWrapperMethods}");
			writer.WriteLine($"\tCOM callable wrapper methods that were forwarded to call base class method : {conversionStats.ForwardedToBaseClassComCallableWrapperMethods}");
		}
		writer.WriteLine();
		writer.WriteLine();
	}

	private static void WriteBasicDataToStdout(NPath statsOutputDirectory)
	{
		ConsoleOutput.Info.WriteLine("Statistics written to : {0}", statsOutputDirectory);
	}
}
