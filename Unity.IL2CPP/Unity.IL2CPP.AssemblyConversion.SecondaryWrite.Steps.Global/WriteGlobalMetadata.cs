using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.TableWriters;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.Global;

public class WriteGlobalMetadata : ScheduledStep
{
	protected override string Name => "Write Global Metadata";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	public void Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		using (CreateProfilerSectionAroundScheduling(scheduler.SchedulingContext, scheduler.WorkIsDoneOnDifferentThread))
		{
			if (!Skip(scheduler.SchedulingContext))
			{
				GlobalSchedulingContext context = scheduler.SchedulingContext;
				TableInfo metadataUsagesTableInfo = Il2CppMetadataUsageTableInfo(context);
				TableInfo typeDefinitionTable = new WriteIl2CppTypeDefinitions(context.Results.PrimaryCollection.Metadata).Schedule(scheduler);
				scheduler.Enqueue(scheduler.QueuingContext, WriteIl2CppMetadataUsage, metadataUsagesTableInfo);
				TableInfo genericInstanceDefinitionTable = new WriteIl2CppGenericInstDefinitions().Schedule(scheduler);
				TableInfo ccTypeValuesTable = new WriteCompilerCalculateTypeValues().Schedule(scheduler);
				TableInfo ccFieldValuesTable = new WriteCompilerCalculateFieldValues().Schedule(scheduler);
				TableInfo genericMethodDefinitionTable = new WriteIl2CppGenericMethodDefinitions(context.Results.SecondaryCollection.GenericInstanceTable, context.Results.PrimaryCollection.Metadata).Schedule(scheduler);
				TableInfo genericMethodTable = new WriteIl2CppGenericMethodTable().Schedule(scheduler);
				TableInfo genericClassTable = new WriteIl2CppGenericClassTable().Schedule(scheduler);
				new WriteIl2CppMetadataRegistration(new TableInfo[8] { genericClassTable, genericInstanceDefinitionTable, genericMethodTable, typeDefinitionTable, genericMethodDefinitionTable, ccFieldValuesTable, ccTypeValuesTable, metadataUsagesTableInfo }.AsReadOnly()).Schedule(scheduler);
			}
		}
	}

	private void WriteIl2CppMetadataUsage(WorkItemData<GlobalWriteContext, TableInfo> data)
	{
		SourceWritingContext context = data.Context.CreateSourceWritingContext();
		using ICppCodeStream writer = context.CreateProfiledSourceWriterInOutputDirectory(FileCategory.Metadata, "Il2CppMetadataUsage.c");
		new MetadataUsageWriter(context, writer).WriteMetadataUsage(data.Tag, context.Global.Results.PrimaryWrite.MetadataUsage);
	}

	private TableInfo Il2CppMetadataUsageTableInfo(GlobalSchedulingContext context)
	{
		IMetadataUsageCollectorResults metadataUsages = context.Results.PrimaryWrite.MetadataUsage;
		if (metadataUsages.UsageCount == 0)
		{
			return TableInfo.Empty;
		}
		if (context.Parameters.EnableDebugger || context.Parameters.EnableReload)
		{
			return new TableInfo(metadataUsages.UsageCount, "void** const", context.Services.ContextScope.ForMetadataGlobalVar("g_MetadataUsages"), externTable: true);
		}
		return TableInfo.Empty;
	}
}
