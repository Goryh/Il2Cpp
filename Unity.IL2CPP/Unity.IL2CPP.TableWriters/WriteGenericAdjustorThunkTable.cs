using System;
using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.TableWriters;

public class WriteGenericAdjustorThunkTable : GeneratedCodeTableWriterBaseChunkedTransformed<CollectMethodTables.GenericMethodAdjustorThunkTableEntry, string>
{
	protected override string TableName => "Il2CppGenericAdjustorThunkTable";

	protected override string CodeTableType => "const Il2CppMethodPointer";

	protected override bool ExternTable => true;

	public override TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		return Schedule(scheduler, scheduler.SchedulingContext.Results.SecondaryCollection.MethodTables.SortedGenericMethodAdjustorThunkTableValues, scheduler.SchedulingContext.InputData.JobCount);
	}

	protected override string FileName(ReadOnlyContext context)
	{
		return TableName + ".c";
	}

	protected override string CodeTableName(GlobalSchedulingContext context)
	{
		return context.Services.ContextScope.ForMetadataGlobalVar("g_Il2CppGenericAdjustorThunks");
	}

	protected override string Transform(ReadOnlyContext context, CollectMethodTables.GenericMethodAdjustorThunkTableEntry item)
	{
		return item.Name(context);
	}

	protected override void WriteDeclarations(SourceWritingContext context, IGeneratedCodeStream writer, ReadOnlyCollection<Tuple<CollectMethodTables.GenericMethodAdjustorThunkTableEntry, string>> allItems)
	{
		foreach (Tuple<CollectMethodTables.GenericMethodAdjustorThunkTableEntry, string> adjustorThunkName in allItems)
		{
			writer.WriteLine($"IL2CPP_EXTERN_C void {adjustorThunkName.Item2} ();");
		}
	}

	protected override void WriteItem(SourceWritingContext context, IGeneratedCodeStream writer, Tuple<CollectMethodTables.GenericMethodAdjustorThunkTableEntry, string> item)
	{
		writer.Write(item.Item2);
	}
}
