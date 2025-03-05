using System;
using Unity.IL2CPP.Api;
using Unity.IL2CPP.Contexts;
using Unity.TinyProfiler;

namespace Unity.IL2CPP.AssemblyConversion;

public static class AssemblyConverter
{
	public static ConversionResults ConvertAssemblies(TinyProfiler2 tinyProfiler, AssemblyConversionInputData data, AssemblyConversionParameters parameters, AssemblyConversionInputDataForTopLevelAccess dataForTopLevel)
	{
		using (tinyProfiler.Section("ConvertAssemblies"))
		{
			data.OutputDir.EnsureDirectoryExists().DeleteContents();
			using AssemblyConversionContext context = AssemblyConversionContext.SetupNew(tinyProfiler, data, parameters, dataForTopLevel);
			try
			{
				return ConvertAssemblies(context, dataForTopLevel.ConversionMode);
			}
			catch (AggregateErrorInformationAlreadyProcessedException)
			{
				throw;
			}
			catch (Exception exception)
			{
				throw ErrorMessageWriter.FormatException(context.StatefulServices.ErrorInformation, exception);
			}
		}
	}

	public static ConversionResults ConvertAssemblies(AssemblyConversionContext context, ConversionMode conversionMode)
	{
		BaseAssemblyConverter.CreateFor(conversionMode).Run(context);
		return new ConversionResults(context.InputData.Assemblies, context.Results.Completion.Stats, context.Results.PrimaryWrite.MatchedAssemblyMethodSourceFiles, context.Results.Completion.LoggedMessages, context.Results.SecondaryWrite.FilesWritten, context.Results.Completion.AnalyticsTable);
	}
}
