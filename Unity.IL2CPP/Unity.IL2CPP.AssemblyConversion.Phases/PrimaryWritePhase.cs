using System;
using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.SpecialOptimizations;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.AssemblyConversion.Phases;

internal static class PrimaryWritePhase
{
	public static void Run(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies, AssemblyDefinition entryAssembly)
	{
		TinyProfilerComponent tinyProfiler = context.Services.TinyProfiler;
		using (tinyProfiler.Section("PrimaryWritePhase"))
		{
			ReadOnlyPerAssemblyPendingResults<GenericContextCollection> pendingGenericContextCollections;
			using (IPhaseWorkScheduler<GlobalWriteContext> scheduler = PhaseWorkSchedulerFactory.ForPrimaryWrite(context))
			{
				using (tinyProfiler.Section("Scheduling"))
				{
					new PhaseSortGenericMethods(context.Collectors.GenericMethodCollector).Schedule(scheduler);
					new PhaseSortMethods(context.Collectors.Methods).Schedule(scheduler);
					new PhaseSortTypes(context.Collectors.TypeCollector).Schedule(scheduler);
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteAssemblies"))
					{
						new WriteAssemblies().Schedule(scheduler, assemblies);
					}
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteDriver"))
					{
						new WriteExecutableDriver(entryAssembly).Schedule(scheduler);
					}
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteGenericMethods"))
					{
						new WriteGenericMethods().Schedule(scheduler);
					}
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteGenericInstanceTypes"))
					{
						new WriteGenericInstanceTypes().Schedule(scheduler);
					}
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteGenericComDefinitions"))
					{
						new WriteGenericComDefinitions().Schedule(scheduler);
					}
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteWindowsRuntimeFactories"))
					{
						new WriteWindowsRuntimeFactories().Schedule(scheduler);
					}
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteComCallableWrappers"))
					{
						new WriteComCallableWrappers().Schedule(scheduler);
					}
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteProjectedInterfacesByCCWs"))
					{
						new WriteProjectedInterfacesByComCallableWrappers().Schedule(scheduler);
					}
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterCollectGenericContextMetadata"))
					{
						pendingGenericContextCollections = new CollectGenericContextMetadata().Schedule(scheduler, assemblies);
					}
				}
			}
			using (tinyProfiler.Section("Build Results"))
			{
				object[] results = PhaseResultBuilder.Complete(context.CreateReadOnlyContext(), new(Func<object>, string)[11]
				{
					(() => context.Collectors.GenericMethodCollector.Complete(), context.Collectors.GenericMethodCollector.GetType().ToString()),
					(() => context.Collectors.Methods.Complete(), context.Collectors.Methods.GetType().ToString()),
					(() => context.Collectors.TypeCollector.Complete(), context.Collectors.TypeCollector.GetType().ToString()),
					(() => context.Collectors.ReversePInvokeWrappers.Complete(), context.Collectors.ReversePInvokeWrappers.GetType().ToString()),
					(() => context.Collectors.TypeMarshallingFunctions.Complete(), context.Collectors.TypeMarshallingFunctions.GetType().ToString()),
					(() => context.Collectors.WrappersForDelegateFromManagedToNative.Complete(), context.Collectors.WrappersForDelegateFromManagedToNative.GetType().ToString()),
					(() => context.Collectors.InteropGuids.Complete(), context.Collectors.InteropGuids.GetType().ToString()),
					(() => context.Collectors.MetadataUsage.Complete(), context.Collectors.MetadataUsage.GetType().ToString()),
					(() => pendingGenericContextCollections.Result, "GenericContextCollection"),
					(() => context.Collectors.Symbols.Complete(), context.Collectors.Symbols.GetType().ToString()),
					(() => context.Collectors.MatchedAssemblyMethodSourceFiles.Complete(), context.Collectors.MatchedAssemblyMethodSourceFiles.GetType().ToString())
				});
				context.Results.SetPrimaryWritePhaseResults(new AssemblyConversionResults.PrimaryWritePhase((IGenericMethodCollectorResults)results[0], (IMethodCollectorResults)results[1], (ITypeCollectorResults)results[2], (IReversePInvokeWrapperCollectorResults)results[3], (ReadOnlyCollection<IIl2CppRuntimeType>)results[4], (ReadOnlyCollection<IIl2CppRuntimeType>)results[5], (ReadOnlyCollection<IIl2CppRuntimeType>)results[6], (IMetadataUsageCollectorResults)results[7], (ReadOnlyDictionary<AssemblyDefinition, GenericContextCollection>)results[8], (ISymbolsCollectorResults)results[9], (ReadOnlyCollection<NPath>)results[10]));
			}
		}
	}
}
