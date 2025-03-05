using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Debugger;

namespace Unity.IL2CPP.AssemblyConversion.Steps;

internal static class SecondaryWriteSteps
{
	public static void WriteDebuggerTables(GlobalWriteContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
	{
		if (!context.Parameters.EnableDebugger)
		{
			return;
		}
		using (context.Services.TinyProfiler.Section("Write debugger tables"))
		{
			foreach (AssemblyDefinition assembly in assemblies)
			{
				DebugWriter.WriteDebugMetadata(context.CreateSourceWritingContext(), assembly, (SequencePointCollector)context.PrimaryCollectionResults.SequencePoints.GetProvider(assembly), context.PrimaryCollectionResults.CatchPoints.GetCollector(assembly));
			}
		}
	}

	public static void WriteUnresolvedIndirectCalls(GlobalWriteContext context, out UnresolvedIndirectCallsTableInfo virtualCallTables)
	{
		using (context.Services.TinyProfiler.Section("WriteUnresolvedStubs"))
		{
			virtualCallTables = UnresolvedIndirectCallStubWriter.WriteUnresolvedStubs(context.CreateSourceWritingContext());
		}
	}
}
