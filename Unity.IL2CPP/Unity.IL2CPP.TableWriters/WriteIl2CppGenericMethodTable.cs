using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.TableWriters;

internal class WriteIl2CppGenericMethodTable : CppCodeTableWriterBaseChunked<CollectMethodTables.GenericMethodTableEntry>
{
	protected override string TableName => "Il2CppGenericMethodTable";

	protected override string CodeTableType => "const Il2CppGenericMethodFunctionsDefinitions";

	protected override bool ExternTable => true;

	protected static string FormatMethodTableEntry(SourceWritingContext context, CollectMethodTables.GenericMethodTableEntry m, ReadOnlyInvokerCollection invokerCollection)
	{
		return "{ " + m.TableIndex + ", " + m.PointerTableIndex + ", " + invokerCollection.GetIndex(context, m.Method.GenericMethod) + ", " + m.AdjustorThunkTableIndex + "}";
	}

	public override TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		return Schedule(scheduler, scheduler.SchedulingContext.Results.SecondaryCollection.MethodTables.SortedGenericMethodTableValues, scheduler.SchedulingContext.InputData.JobCount);
	}

	protected override string CodeTableName(GlobalSchedulingContext context)
	{
		return context.Services.ContextScope.ForMetadataGlobalVar("g_Il2CppGenericMethodFunctions");
	}

	protected override void WriteDeclarations(SourceWritingContext context, ICppCodeStream writer, ReadOnlyCollection<CollectMethodTables.GenericMethodTableEntry> allItems)
	{
	}

	protected override void WriteItem(SourceWritingContext context, ICppCodeStream writer, CollectMethodTables.GenericMethodTableEntry item)
	{
		writer.Write(FormatMethodTableEntry(context, item, context.Global.Results.SecondaryCollection.Invokers));
	}
}
