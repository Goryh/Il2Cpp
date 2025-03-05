using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.Steps;

internal static class SetupSteps
{
	public static void UpdateCodeConversionCache(AssemblyConversionContext context)
	{
		if (context.Parameters.CodeConversionCache)
		{
			CodeConversionCache codeConversionCache = new CodeConversionCache(context);
			if (!codeConversionCache.IsUpToDate())
			{
				codeConversionCache.Refresh();
			}
		}
	}

	public static void CreateDataDirectory(AssemblyConversionContext context)
	{
		context.InputData.DataFolder.EnsureDirectoryExists();
	}

	public static void RegisterCorlib(AssemblyConversionContext context, bool includeWindowsRuntime)
	{
		using (context.Services.TinyProfiler.Section("RegisterCorlib"))
		{
			context.StatefulServices.Diagnostics.Initialize(context);
			context.Services.TypeProvider.Initialize(context, context.Services.DataModel.TypeContext);
			if (includeWindowsRuntime)
			{
				context.Services.WindowsRuntimeProjections.Initialize(context.CreatePrimaryCollectionContext(), context.Services.DataModel.TypeContext);
			}
			context.Services.ICallMapping.Initialize(context);
		}
	}

	public static void WriteResources(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
	{
		using (context.Services.TinyProfiler.Section("WriteResources"))
		{
			WriteEmbeddedResourcesForEachAssembly(context, assemblies);
		}
	}

	private static void WriteEmbeddedResourcesForEachAssembly(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
	{
		NPath resourcesOutputDirectory = context.InputData.DataFolder.Combine("Resources").MakeAbsolute();
		resourcesOutputDirectory.CreateDirectory();
		foreach (AssemblyDefinition usedAssembly in assemblies)
		{
			if (usedAssembly.MainModule.Resources.Any())
			{
				using FileStream file = new FileStream(resourcesOutputDirectory.Combine(usedAssembly.MainModule.GetModuleFileName() + "-resources.dat").ToString(), FileMode.Create, FileAccess.Write);
				ResourceWriter.WriteEmbeddedResources(usedAssembly, file);
			}
		}
	}
}
