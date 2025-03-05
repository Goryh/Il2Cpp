using System;
using System.Diagnostics;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Analytics;
using Unity.IL2CPP.Api.Output.Analytics;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.Steps;

public static class CompletionSteps
{
	[Conditional("DEBUG")]
	public static void WriteDataModelStatistics(GlobalReadOnlyContext context, TypeContext typeContext)
	{
		if (!context.Parameters.EnableStats)
		{
			return;
		}
		using (context.Services.TinyProfiler.Section("WriteDataModelStatistics"))
		{
		}
	}

	public static Il2CppDataTable FinalizeAnalytics(GlobalReadOnlyContext context)
	{
		if (!context.Parameters.EnableAnalytics)
		{
			return new Il2CppDataTable();
		}
		using (context.Services.TinyProfiler.Section("Finalize Analytics"))
		{
			return AnalyticsTableBuilder.Complete(context);
		}
	}

	public static void CopyAdditionalCppFiles(AssemblyConversionContext context)
	{
		using (context.Services.TinyProfiler.Section("CopyAdditionalCppFiles"))
		{
			string[] assemblyFileNames = context.Results.SecondaryWrite.FilesWritten.PerAssembly.Select((NPath file) => file.FileName).ToArray();
			NPath[] additionalCpp = context.InputData.AdditionalCpp;
			foreach (NPath cpp in additionalCpp)
			{
				string cppFileName = cpp.FileName;
				NPath otherCppFileWithSameName = context.InputData.AdditionalCpp.FirstOrDefault((NPath otherCpp) => otherCpp != cpp && otherCpp.FileName == cppFileName);
				if (otherCppFileWithSameName != null)
				{
					throw new Exception("There are two cpp-plugins with the same name. Each cpp-plugin must have a unique name. One is at " + cpp.InQuotes() + ", the other at " + otherCppFileWithSameName.InQuotes());
				}
				if (assemblyFileNames.Any((string assemblyFileName) => assemblyFileName == cppFileName))
				{
					throw new Exception("There is an assembly with the same name as the cpp plugin at " + cpp.InQuotes() + ". Managed plugins and cpp plugins must have unique names.");
				}
				cpp.Copy(context.InputData.OutputDir);
			}
		}
	}
}
