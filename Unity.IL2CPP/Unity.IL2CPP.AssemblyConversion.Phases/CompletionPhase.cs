using Unity.IL2CPP.Api.Output.Analytics;
using Unity.IL2CPP.AssemblyConversion.Steps;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Components;

namespace Unity.IL2CPP.AssemblyConversion.Phases;

internal static class CompletionPhase
{
	public static void Run(AssemblyConversionContext context)
	{
		TinyProfilerComponent tinyProfiler = context.Services.TinyProfiler;
		using (tinyProfiler.Section("CompletionPhase"))
		{
			CompletionSteps.CopyAdditionalCppFiles(context);
			Il2CppDataTable analyticsTable = CompletionSteps.FinalizeAnalytics(context.GlobalReadOnlyContext);
			using (tinyProfiler.Section("Build Results"))
			{
				context.Results.SetCompletionPhaseResults(new AssemblyConversionResults.CompletionPhase(context.Collectors.Stats, context.StatefulServices.MessageLogger.Complete(), analyticsTable));
			}
		}
	}
}
