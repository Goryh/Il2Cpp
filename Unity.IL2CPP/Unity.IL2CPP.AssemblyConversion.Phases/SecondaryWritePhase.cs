using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.Steps;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.Phases;

internal static class SecondaryWritePhase
{
	public static void Run(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies, bool includeMetadata = true)
	{
		using (context.Services.TinyProfiler.Section("SecondaryWritePhase"))
		{
			Part1(context, assemblies);
			Part3(context);
			Part4(context, assemblies, includeMetadata);
		}
	}

	private static void Part1(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
	{
		TinyProfilerComponent tinyProfiler = context.Services.TinyProfiler;
		using (tinyProfiler.Section("Part1"))
		{
			if (context.Parameters.EnableDebugger)
			{
				using (tinyProfiler.Section("Scheduling"))
				{
					using IPhaseWorkScheduler<GlobalWriteContext> scheduler = PhaseWorkSchedulerFactory.ForSecondaryWrite(context);
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteDebuggerTables"))
					{
						new WriteDebuggerTables().Schedule(scheduler, assemblies);
					}
				}
			}
			using (tinyProfiler.Section("Build Results"))
			{
				context.Results.SetSecondaryWritePhasePart1Results(new AssemblyConversionResults.SecondaryWritePhasePart1(context.Collectors.IndirectCalls.Complete()));
			}
		}
	}

	private static void Part3(AssemblyConversionContext context)
	{
		TinyProfilerComponent tinyProfiler = context.Services.TinyProfiler;
		using (tinyProfiler.Section("Part3"))
		{
			UnresolvedIndirectCallsTableInfo virtualCallTables;
			using (tinyProfiler.Section("Scheduling"))
			{
				using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteUnresolvedStubs"))
				{
					SecondaryWriteSteps.WriteUnresolvedIndirectCalls(context.GlobalWriteContext, out virtualCallTables);
				}
			}
			using (tinyProfiler.Section("Build Results"))
			{
				context.Results.SetSecondaryWritePhasePart3Results(new AssemblyConversionResults.SecondaryWritePhasePart3(virtualCallTables));
			}
		}
	}

	private static void Part4(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies, bool includeMetadata = true)
	{
		TinyProfilerComponent tinyProfiler = context.Services.TinyProfiler;
		using (tinyProfiler.Section("Part4"))
		{
			using (tinyProfiler.Section("Scheduling"))
			{
				if (includeMetadata)
				{
					using IPhaseWorkScheduler<GlobalWriteContext> scheduler = PhaseWorkSchedulerFactory.ForSecondaryWrite(context);
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteGlobalMetadataDat"))
					{
						new WriteGlobalMetadataDat().Schedule(scheduler);
					}
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteCodeMetadata"))
					{
						new WritePerAssemblyCodeMetadata().Schedule(scheduler, assemblies);
					}
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteMetadata"))
					{
						new WriteGlobalMetadata().Schedule(scheduler);
					}
					new WriteMethodMap(assemblies).Schedule(scheduler, context.Results.SecondaryCollection.GenericMethodPointerNameTable?.Items);
					new WriteLineMapping().Schedule(scheduler);
				}
			}
			using (tinyProfiler.Section("Build Results"))
			{
				context.Results.SetSecondaryWritePhaseResults(new AssemblyConversionResults.SecondaryWritePhase(context.StatefulServices.PathFactory.Complete()));
			}
		}
	}
}
