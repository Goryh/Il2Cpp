using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.Steps;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.Phases;

internal static class SetupPhase
{
	public static void Run(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies, bool includeWindowsRuntime = true)
	{
		TinyProfilerComponent tinyProfiler = context.Services.TinyProfiler;
		using (tinyProfiler.Section("SetupPhase"))
		{
			SetupSteps.UpdateCodeConversionCache(context);
			SetupSteps.RegisterCorlib(context, includeWindowsRuntime);
			SetupSteps.CreateDataDirectory(context);
			SetupSteps.WriteResources(context, assemblies);
			context.Services.DataModel.TypeContext.FreezeDefinitions();
			using (tinyProfiler.Section("Build Results"))
			{
				context.Results.SetSetupPhaseResults(new AssemblyConversionResults.SetupPhase(context.Collectors.RuntimeImplementedMethodWriterCollector.Complete()));
			}
		}
	}
}
